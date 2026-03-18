---
name: create-functional-core-csharp
description: "Use when creating, refactoring, or reviewing a pure functional core. Trigger phrases: functional core, pure domain logic, Capable.Monads, Result pipeline, domain events, domain errors, Bind, Map, deterministic rules."
---

# Create Functional Core

Use this skill when the goal is to encode business decisions as pure, deterministic logic with explicit success and failure values.

This repository's conventions:

- `Result<TSuccess, TFailure>` generic order is success first, failure second.
- The functional core returns domain results, typically domain events on success and strongly-typed domain errors on failure.
- The core should not perform I/O or integration orchestration. For that boundary behavior, see [Create Imperative Shell](../create-imperative-shell/SKILL.md).

Reference examples in this repo:

- `references/functional-core-example.md`
- `../../references/result-api.md`

## Outcome

Produce a design or implementation with these properties:

1. A pure core that encodes business rules and state transitions.
2. Strongly-typed success and failure values instead of exceptions for expected business outcomes.
3. Specs that prove the core is pure and deterministic.

## Workflow

### 1. Model the domain language first

Define domain-facing types before wiring orchestration:

- Commands and validated commands
- Authorization or actor types
- Domain errors as explicit records or discriminated shapes
- Domain events or other success outputs
- A context object when multiple values travel together

Prefer names that reflect the business language, not transport or infrastructure language.

Example:

```csharp
public abstract record DomainError
{
    public record UnauthorizedAction(string Reason) : DomainError;
    public record BusinessRuleViolation(string Rule) : DomainError;
}

public abstract record DomainEvent
{
    public record OrderPlaced(string OrderId, int Quantity) : DomainEvent;
}

public sealed class CommandContext
{
    public ValidatedCommand Command { get; }
    public DomainEvent[] History { get; }
    public Authorized User { get; }

    public CommandContext(ValidatedCommand command, DomainEvent[] history, Authorized user)
    {
        Command = command;
        History = history;
        User = user;
    }
}
```

### 2. Split pure decisions from effectful operations

Ask of each step: does it require time, I/O, external state, randomness, or framework services?

- If yes, it belongs in the shell.
- If no, and it is a business rule or state transition, it belongs in the core.

Keep the core free of:

- `Task` unless the computation truly needs asynchronous effects
- Database access
- HTTP calls
- logging as part of decision logic
- direct clock or GUID access unless injected as data

### 3. Build a pure decision object or function

The core should take explicit input and return `Result<TSuccess, TFailure>`.

Typical shape in this repository:

```csharp
public sealed class OrderDecision
{
    public Result<DomainEvent[], DomainError> Execute(CommandContext context)
    {
        return from _ in ValidateBusinessRules(context)
               from events in ComputeStateChanges(context)
               select events;
    }

    private Result<Unit, DomainError> ValidateBusinessRules(CommandContext context)
    {
        if (context.Command.Quantity <= 0)
        {
            return Result<Unit, DomainError>.Failure(
                new DomainError.BusinessRuleViolation("Orders must have positive quantity"));
        }

        return Result<Unit, DomainError>.Success(new Unit());
    }

    private Result<DomainEvent[], DomainError> ComputeStateChanges(CommandContext context)
    {
        return Result<DomainEvent[], DomainError>.Success(
        [
            new DomainEvent.OrderPlaced(context.Command.OrderId, context.Command.Quantity)
        ]);
    }
}
```

Guidelines:

- Return failures for expected business problems.
- Emit success values that describe what the business decided, not what infrastructure did.
- Prefer composing several small pure functions with `Map`, `Bind`, and LINQ syntax.

### 4. Spec core behavior directly

Write specs that focus on core behavior only.

Core specs should prove:

- no mocks are needed
- business rules fail with strongly-typed domain errors
- success returns the expected domain events
- identical input yields identical output

Minimal checklist:

```text
[ ] Core entry point returns Result<TSuccess, TFailure>
[ ] Core code has no I/O or framework dependencies
[ ] Success values are domain outputs, not response DTOs
[ ] Failure values are explicit, typed, and meaningful
[ ] Composition uses Bind/Map/LINQ instead of nested branching where practical
[ ] Specs cover deterministic outcomes for both success and failure
```

## Decision Points

### When to use a context object

Create a context type when the core needs several related inputs that conceptually travel together. This reduces parameter sprawl and makes the core's contract clearer.

### When to emit events vs return a final aggregate

- Emit events when the core's responsibility is deciding what happened.
- Return another domain value when the result is a computed decision rather than an event stream.

### When to stay synchronous

If the computation is pure, keep it synchronous even if the shell around it is async. Convert to async only at the composition boundary with `ToAsync()`.

### When to use `Map` vs `Bind`

- Use `Map` when transforming a success value without introducing another `Result`.
- Use `Bind` when the next step can fail and already returns `Result<...>`.

## Review Criteria

Flag the design if any of these are true:

- The core reaches into repositories, HTTP clients, clocks, or logging frameworks.
- Exceptions are being used for expected domain decisions.
- Failure cases are represented only as strings when a typed domain error would be clearer.
- Specs for business logic require mocks or async setup.

## Example Prompts

- "Create a pure functional core for placing an order using Capable.Monads and explicit domain errors."
- "Refactor this business logic so rules return Result<DomainEvent[], DomainError> with no I/O dependencies."
- "Review this domain decision code for functional purity and typed failure modeling."
- "Add specs that prove identical input produces identical domain decisions."

## Output Expectations

When using this skill, produce:

1. A proposed domain model.
2. The pure core API and implementation.
3. Specs for core purity and deterministic behavior.
4. A short explanation of why each concern belongs in the core.