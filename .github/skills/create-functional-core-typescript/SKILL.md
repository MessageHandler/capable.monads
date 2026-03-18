---
name: create-functional-core-typescript
description: "Use when creating, refactoring, or reviewing a pure functional core in TypeScript with Capable.Monads Result pipelines, typed domain errors/events, bind/map composition, and deterministic rules. Trigger phrases: functional core typescript, pure domain logic ts, result pipeline ts, railroad programming ts, domain decisions ts."
---

# Create Functional Core (TypeScript)

Use this skill when implementing business decisions as pure, deterministic TypeScript logic.

Repository conventions for TypeScript:

- `Result<TSuccess, TFailure>` generic order is success first, failure second.
- Prefer discriminated unions for `DomainError` and `DomainEvent`.
- Compose with `bind` and `map`; TypeScript has no LINQ query syntax.
- Keep the functional core synchronous unless pure computation truly requires async.

References in this repo:

- `../references/typescript-functional-core-example.md`
- `../references/typescript-result-api.md`

## Outcome

Produce a functional core with these properties:

1. Pure domain decision logic with explicit inputs/outputs.
2. Typed success and failure values with no exceptions for expected business outcomes.
3. Deterministic behavior proven by tests.

## Workflow

### 1. Model domain language first

Define explicit domain-facing types:

- Command and validated command
- Domain errors as discriminated unions
- Domain events as discriminated unions
- Context object when multiple values travel together

Example discriminated union style:

```ts
type DomainError =
  | { type: "UnauthorizedAction"; reason: string }
  | { type: "BusinessRuleViolation"; rule: string };

type DomainEvent = { type: "OrderPlaced"; orderId: string; quantity: number };
```

### 2. Keep core pure

Core must not perform:

- HTTP/database/file I/O
- Logging as a decision dependency
- Time/random/global-state access

### 3. Compose decisions with `bind` / `map`

Use `bind` when next step can fail and returns `Result`.
Use `map` when next step is a pure transformation.

### 4. Spec behavior directly

Tests should verify:

- same input gives same output
- rule violations return typed errors
- success returns expected domain events
- no mocks required for core tests

## Review Criteria

Flag designs if:

- Core touches infrastructure concerns
- Expected business outcomes are exceptions
- Failures are weakly typed strings where unions would be clearer
- Core tests require async setup or test doubles
