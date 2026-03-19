# TypeScript Imperative Shell Example

This example shows orchestration of effectful steps while delegating business decisions to a pure core.

## Goal

Use `Result` composition to orchestrate:

1. Validation
2. Authorization
3. Loading state
4. Delegating to functional core
5. Persistence

## Example

```ts
import { bindAsync, failure, toAsync, type Result, type Unit, unit } from "@messagehandler/capable.monads";

type DomainError = { type: "BusinessRuleViolation"; rule: string };
type DomainEvent = { type: "OrderPlaced"; orderId: string; quantity: number };
type Command = { commandType: "PlaceOrder" };
type ValidatedCommand = { orderId: string; quantity: number };
type Authorized = { canPlaceOrders: true };

class OrderDecision {
  execute(input: { command: ValidatedCommand; history: readonly DomainEvent[]; user: Authorized }): Result<readonly DomainEvent[], DomainError> {
    return input.command.quantity > 0
      ? { type: "Success", value: [{ type: "OrderPlaced", orderId: input.command.orderId, quantity: input.command.quantity }] }
      : failure({ type: "BusinessRuleViolation", rule: "Orders must have positive quantity" });
  }
}

class CommandHandler {
  constructor(
    private readonly validateAsync: (command: Command) => Promise<Result<ValidatedCommand, DomainError>>,
    private readonly authorizeAsync: () => Promise<Result<Authorized, DomainError>>,
    private readonly loadAsync: (command: ValidatedCommand) => Promise<Result<readonly DomainEvent[], DomainError>>,
    private readonly persistAsync: (events: readonly DomainEvent[]) => Promise<Result<Unit, DomainError>>,
    private readonly decision: OrderDecision,
  ) {}

  async handleAsync(command: Command): Promise<Result<readonly DomainEvent[], DomainError>> {
    const validated = await this.validateAsync(command);
    const authorized = await bindAsync(validated, async () => this.authorizeAsync());
    const history = await bindAsync(authorized, async () => this.loadAsync({ orderId: "order-001", quantity: 1 }));
    const decided = await bindAsync(history, async (loaded) =>
      toAsync(this.decision.execute({ command: { orderId: "order-001", quantity: 1 }, history: loaded, user: { canPlaceOrders: true } })),
    );

    return bindAsync(decided, async (events) => {
      const persisted = await this.persistAsync(events);
      return persisted.type === "Success" ? { type: "Success", value: events } : { type: "Failure", error: persisted.error };
    });
  }
}
```

## Shell Checklist

- All effects are in shell
- Core remains pure
- Failures short-circuit
- No downstream side effects after failure
