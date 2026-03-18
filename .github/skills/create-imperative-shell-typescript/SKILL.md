---
name: create-imperative-shell-typescript
description: "Use when creating, refactoring, or reviewing a TypeScript imperative shell around a pure functional core with Capable.Monads Result pipelines, short-circuiting orchestration, async boundaries, and dependency-driven I/O. Trigger phrases: imperative shell typescript, command handler ts, application service ts, orchestration ts, result pipeline async ts."
---

# Create Imperative Shell (TypeScript)

Use this skill when orchestrating effectful boundaries in TypeScript while delegating business decisions to a pure core.

Repository conventions for TypeScript:

- `Result<TSuccess, TFailure>` generic order is success first, failure second.
- Compose orchestration with `bind`, `bindAsync`, `map`, `mapFailure`, and `toAsync`.
- No LINQ query syntax in TypeScript; keep pipelines explicit and linear.

References in this repo:

- `../references/typescript-imperative-shell-example.md`
- `../references/typescript-result-api.md`

## Outcome

Produce an imperative shell with these properties:

1. All side effects orchestrated in one readable flow.
2. Core business decisions delegated to pure functional core.
3. Failures short-circuit naturally with `Result`.
4. Infrastructure failures translated into stable application/domain errors.
5. Tests prove ordering and no-extra-work guarantees.

## Workflow

### 1. Classify steps

Separate each use-case step into:

- Effectful boundary work (validation, auth, load, persist, publish)
- Pure business decisions (state transition logic)

### 2. Inject dependencies

Use narrow dependencies for shell orchestration:

- validation service
- authorization service
- repository/event store
- pure decision object/function

### 3. Compose the flow explicitly

Typical pattern:

1. `validated = await validateAsync(command)`
2. `authorized = await bindAsync(validated, ...)`
3. `history = await bindAsync(authorized, ...)`
4. `events = await bindAsync(history, h => toAsync(decision.execute(...)))`
5. `persisted = await bindAsync(events, persist)`

### 4. Translate errors at boundaries

- Domain failures should come from the core.
- Infrastructure failures should be mapped before returning.

### 5. Verify orchestration in tests

Tests should prove:

- dependency call order
- early failure short-circuits later work
- decision logic remains in core

## Review Criteria

Flag designs if:

- Shell contains business-rule calculations
- Failure path still triggers downstream effects
- Transport/vendor errors leak through API contracts
- Tests assert final value only and ignore orchestration behavior
