import { expect } from "chai";
import {
  bind,
  failure,
  fold,
  isFailure,
  isSuccess,
  map,
  success,
  toAsync,
  type Result,
  type Unit,
  unit,
} from "../src/index.js";

type DomainError =
  | { type: "InsufficientInventory"; requested: number; available: number }
  | { type: "UnauthorizedAction"; reason: string }
  | { type: "BusinessRuleViolation"; rule: string };

type DomainEvent =
  | { type: "OrderPlaced"; orderId: string; quantity: number }
  | { type: "InventoryReserved"; quantity: number };

type ValidatedCommand = {
  readonly orderId: string;
  readonly quantity: number;
};

type Authorized = { readonly canPlaceOrders: true };
type User = { readonly userId: string };
type Command = { readonly commandType: "PlaceOrder" };

type CommandContext = {
  readonly command: ValidatedCommand;
  readonly history: readonly DomainEvent[];
  readonly user: Authorized;
};

class OrderDecision {
  execute(context: CommandContext): Result<readonly DomainEvent[], DomainError> {
    return bind(this.validateBusinessRules(context), () => this.computeStateChanges(context));
  }

  private validateBusinessRules(context: CommandContext): Result<Unit, DomainError> {
    if (context.command.quantity <= 0) {
      return failure({ type: "BusinessRuleViolation", rule: "Orders must have positive quantity" });
    }

    return success<Unit, DomainError>(unit);
  }

  private computeStateChanges(context: CommandContext): Result<readonly DomainEvent[], DomainError> {
    return success([
      { type: "OrderPlaced", orderId: context.command.orderId, quantity: context.command.quantity },
      { type: "InventoryReserved", quantity: context.command.quantity },
    ]);
  }
}

class ValidationService {
  async validateAsync(_command: Command): Promise<Result<ValidatedCommand, DomainError>> {
    return success({ orderId: "order-001", quantity: 1 });
  }
}

class AuthorizationService {
  async authorizeAsync(_user: User): Promise<Result<Authorized, DomainError>> {
    return success({ canPlaceOrders: true });
  }
}

class EventStore {
  async loadEventsAsync(_command: ValidatedCommand): Promise<Result<readonly DomainEvent[], DomainError>> {
    return success([]);
  }

  async persistEventsAsync(_events: readonly DomainEvent[]): Promise<Result<Unit, DomainError>> {
    return success(unit);
  }
}

class CommandHandler {
  private readonly decision: OrderDecision;

  constructor(
    private readonly validationService: ValidationService,
    private readonly authorizationService: AuthorizationService,
    private readonly eventStore: EventStore,
  ) {
    this.decision = new OrderDecision();
  }

  async handleAsync(command: Command): Promise<Result<readonly DomainEvent[], DomainError>> {
    const validated = await this.validationService.validateAsync(command);
    const authorized = await bindAsyncResult(validated, () => this.authorizationService.authorizeAsync({ userId: "user-1" }));
    const history = await bindAsyncResult(authorized, () => this.eventStore.loadEventsAsync({ orderId: "order-001", quantity: 1 }));

    const events = await bindAsyncResult(history, (loadedHistory) =>
      toAsync(
        this.decision.execute({
          command: { orderId: "order-001", quantity: 1 },
          history: loadedHistory,
          user: { canPlaceOrders: true },
        }),
      ),
    );

    const persisted = await bindAsyncResult(events, (emitted) => this.eventStore.persistEventsAsync(emitted));

    return map(persisted, () => fold(events, () => [], (emitted) => emitted));
  }
}

const bindAsyncResult = async <TSuccess, TFailure, TNextSuccess>(
  result: Result<TSuccess, TFailure>,
  binder: (value: TSuccess) => Promise<Result<TNextSuccess, TFailure>>,
): Promise<Result<TNextSuccess, TFailure>> => {
  if (isFailure(result)) {
    return failure(result.error);
  }

  return binder(result.value);
};

describe("Functional core / imperative shell example", () => {
  it("functional core validates business rules", () => {
    const decision = new OrderDecision();
    const invalidContext: CommandContext = {
      command: { orderId: "123", quantity: 0 },
      history: [],
      user: { canPlaceOrders: true },
    };

    const result = decision.execute(invalidContext);

    expect(isFailure(result)).to.equal(true);
    expect(fold(result, (error) => error.type, () => "ok")).to.equal("BusinessRuleViolation");
  });

  it("functional core is pure and testable", () => {
    const decision = new OrderDecision();
    const context: CommandContext = {
      command: { orderId: "123", quantity: 5 },
      history: [],
      user: { canPlaceOrders: true },
    };

    const result = decision.execute(context);

    expect(isSuccess(result)).to.equal(true);
    const hasOrderPlaced = fold(
      result,
      () => false,
      (events) => events.some((event) => event.type === "OrderPlaced"),
    );
    expect(hasOrderPlaced).to.equal(true);
  });

  it("imperative shell orchestrates effects and delegates to core", async () => {
    const shell = new CommandHandler(new ValidationService(), new AuthorizationService(), new EventStore());

    const result = await shell.handleAsync({ commandType: "PlaceOrder" });

    expect(isSuccess(result)).to.equal(true);
  });

  it("imperative shell short-circuits on validation failure", async () => {
    let authorizeCalled = false;

    class FailingValidationService extends ValidationService {
      override async validateAsync(_command: Command): Promise<Result<ValidatedCommand, DomainError>> {
        return failure({ type: "BusinessRuleViolation", rule: "Validation failed" });
      }
    }

    class TrackingAuthorizationService extends AuthorizationService {
      override async authorizeAsync(_user: User): Promise<Result<Authorized, DomainError>> {
        authorizeCalled = true;
        return success({ canPlaceOrders: true });
      }
    }

    const shell = new CommandHandler(new FailingValidationService(), new TrackingAuthorizationService(), new EventStore());

    const result = await shell.handleAsync({ commandType: "PlaceOrder" });

    expect(isFailure(result)).to.equal(true);
    expect(authorizeCalled).to.equal(false);
  });

  it("functional core returns strongly typed domain errors", () => {
    const decision = new OrderDecision();
    const invalidContext: CommandContext = {
      command: { orderId: "123", quantity: -5 },
      history: [],
      user: { canPlaceOrders: true },
    };

    const result = decision.execute(invalidContext);

    const isExpectedError = fold(
      result,
      (error) => error.type === "BusinessRuleViolation" && error.rule.includes("positive"),
      () => false,
    );
    expect(isExpectedError).to.equal(true);
  });
});
