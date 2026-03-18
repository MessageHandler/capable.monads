# TypeScript Functional Core Example

This example shows a pure domain decision in TypeScript.

## Goal

Keep domain decisions deterministic and side-effect free while returning:

- `Result<DomainEvent[], DomainError>` on success/failure
- Typed domain events and typed domain errors

## Example

```ts
import { bind, failure, success, type Result, type Unit, unit } from "@messagehandler/capable.monads";

type DomainError =
  | { type: "BusinessRuleViolation"; rule: string }
  | { type: "UnauthorizedAction"; reason: string };

type DomainEvent =
  | { type: "OrderPlaced"; orderId: string; quantity: number }
  | { type: "InventoryReserved"; quantity: number };

type CommandContext = {
  command: { orderId: string; quantity: number };
  history: readonly DomainEvent[];
  user: { canPlaceOrders: true };
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
```

## Purity Checklist

- No I/O
- No wall-clock reads
- No random generation
- No framework dependencies
- Same input always returns same output
