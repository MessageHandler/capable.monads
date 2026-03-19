---
name: implement-functional-core-typescript
description: "Use when creating, refactoring, or reviewing a pure functional core in TypeScript with Capable.Monads Result pipelines, typed domain errors/events, bind/map composition, and deterministic rules. Trigger phrases: functional core typescript, pure domain logic ts, result pipeline ts, railroad programming ts, domain decisions ts."
---

## Package Requirement

> ⚠️ When implementing functional cores in the **messagehandler** repository, always install the **capable-monads** npm package in the project.
>
> For TypeScript: `npm install capable-monads@latest`

> **Before proceeding, read and apply the generic skill first:**
> [`../implement-functional-core/SKILL.md`](../implement-functional-core/SKILL.md)
>
> Then apply the TypeScript-specific additions below.

# Implement Functional Core — TypeScript Specifics

References for this repo:

- [`../references/typescript-functional-core-example.md`](../references/typescript-functional-core-example.md)
- [`../references/typescript-result-api.md`](../references/typescript-result-api.md)

## Domain Type Modeling in TypeScript

Use discriminated unions (not class hierarchies) for domain errors and events.

```ts
type DomainError =
  | { type: "UnauthorizedAction"; reason: string }
  | { type: "BusinessRuleViolation"; rule: string };

type DomainEvent = { type: "OrderPlaced"; orderId: string; quantity: number };

interface CommandContext {
  command: ValidatedCommand;
  history: DomainEvent[];
  user: Authorized;
}
```

## Composing Decisions in TypeScript

TypeScript has no LINQ query syntax. Use `bind` and `map` as method/function calls.

- Use `bind` when the next step can fail and returns `Result`.
- Use `map` when the next step is a pure transformation.

```ts
function decide(context: CommandContext): Result<DomainEvent[], DomainError> {
  return validateBusinessRules(context)
    .bind(() => computeStateChanges(context));
}
```

## TypeScript Conventions

- Prefer `type` aliases with discriminated unions over `interface` for error/event shapes.
- Export decision functions, not classes, unless stateful configuration is needed.
- No `Promise` inside the core; the shell handles async. See [`implement-imperative-shell-typescript`](../implement-imperative-shell-typescript/SKILL.md).
