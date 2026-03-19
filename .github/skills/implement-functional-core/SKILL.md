---
name: implement-functional-core
description: "Use when creating, refactoring, or reviewing a pure functional core in any language. Trigger phrases: functional core, pure domain logic, Result pipeline, domain events, domain errors, Bind, Map, deterministic rules, railroad programming."
---

# Implement Functional Core

Use this skill when the goal is to encode business decisions as pure, deterministic logic with explicit success and failure values.

**If you know the target language, also read the language-specific supplement:**
- C#: [`implement-functional-core-csharp/SKILL.md`](../implement-functional-core-csharp/SKILL.md)
- TypeScript: [`implement-functional-core-typescript/SKILL.md`](../implement-functional-core-typescript/SKILL.md)

Repository conventions (all languages):

- `Result<TSuccess, TFailure>` generic order is success first, failure second.
- The functional core returns domain results: domain events on success, strongly-typed domain errors on failure.
- The core must not perform I/O or integration orchestration. For that boundary, see the imperative shell skill.
- Reuse existing slice contract types in the core when they already represent the same domain command/event; do not introduce duplicate `*Input`/`*Output` aliases with identical structure.
- When contract types live in another layer/package, reference that layer through its package (for example `capabilityname.capability.contracts`) instead of importing another layer's source files via relative paths.

## Outcome

Produce a design or implementation with these properties:

1. A pure core that encodes business rules and state transitions.
2. Strongly-typed success and failure values instead of exceptions for expected business outcomes.
3. Specs that prove the core is pure and deterministic.

## Workflow

### 1. Model the domain language first

Define domain-facing types before wiring any orchestration:

- Commands and validated commands
- Authorization or actor types
- Domain errors as explicit, named shapes (discriminated unions or sealed hierarchies)
- Domain events or other success outputs
- A context object when multiple values travel together

Prefer names that reflect the business language, not transport or infrastructure language.

### 2. Split pure decisions from effectful operations

Ask of each step: does it require time, I/O, external state, randomness, or framework services?

- **Yes** → belongs in the imperative shell.
- **No, it's a business rule or state transition** → belongs in the functional core.

Keep the core free of:

- Database access
- HTTP calls
- Logging as a decision dependency
- Direct clock or GUID access (unless injected as plain data)
- Asynchronous effects unless the computation truly requires them

### 3. Build a pure decision function

The core takes explicit input and returns `Result<TSuccess, TFailure>`.

Typical shape:

```
function decide(context: Context): Result<DomainEvent[], DomainError>
  validate business rules     → Result<Unit, DomainError>
  compute state changes       → Result<DomainEvent[], DomainError>
  compose with bind/map
```

Guidelines:

- Return failures for expected business problems.
- Emit success values that describe what the business decided, not what infrastructure did.
- Prefer composing several small pure functions with `Map` and `Bind` rather than nested branching.

### 4. Spec core behavior directly

Write specs that focus exclusively on core behavior.

Core specs must prove:

- No test doubles (mocks/stubs) are needed
- Business rule violations return strongly-typed domain errors
- Success returns the expected domain events
- Identical input yields identical output

## Decision Points

### When to use a context object

Create a context type when the core needs several related inputs that conceptually travel together. This reduces parameter sprawl and makes the core's contract explicit.

### When to emit events vs return a computed value

- Emit events when the core's responsibility is deciding *what happened*.
- Return another domain value when the result is a computed decision rather than an event stream.

### When to stay synchronous

If computation is pure, keep it synchronous even if the shell around it is async. Convert to async only at the shell's composition boundary.

### When to use `Map` vs `Bind`

- Use `Map` when transforming a success value without introducing another `Result`.
- Use `Bind` when the next step can fail and already returns `Result<...>`.

## Review Criteria

Flag the design if any of these are true:

- The core reaches into repositories, HTTP clients, clocks, or logging frameworks.
- Exceptions are used for expected domain decisions.
- Failure cases are weakly typed strings where a named domain error type would be clearer.
- Specs for business logic require mocks or async setup.
- The core defines duplicate input/output types that copy existing contract types instead of reusing them directly.

## Completion Checklist

```
[ ] Core entry point returns Result<TSuccess, TFailure>
[ ] Core code has no I/O or framework dependencies
[ ] Success values are domain outputs, not response DTOs
[ ] Failure values are explicit, typed, and meaningful
[ ] Composition uses Bind/Map instead of nested branching where practical
[ ] Specs cover deterministic outcomes for both success and failure paths
```

## Output Expectations

When using this skill, produce:

1. A proposed domain model (errors, events, context types).
2. The pure core API and implementation.
3. Specs for core purity and deterministic behavior.
4. A short explanation of why each concern belongs in the core.
