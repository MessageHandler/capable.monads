---
name: functional-core-capable-monads
description: "Use when creating, refactoring, or reviewing a functional core / imperative shell with Capable.Monads Result<TSuccess, TFailure>. Trigger phrases: functional core, imperative shell, Capable.Monads, Result pipeline, domain events, domain errors, short-circuiting command handler, Bind, Map, ToAsync."
---

# Functional Core with Capable.Monads

Use this skill when the goal is to keep business decisions pure and push I/O, async work, and integration concerns to the application boundary.

This repository's conventions:

- `Result<TSuccess, TFailure>` generic order is success first, failure second.
- The functional core returns domain results, typically domain events on success and strongly-typed domain errors on failure.
- The imperative shell orchestrates validation, authorization, loading, persistence, and other effects with LINQ query syntax over `Result` and `Task<Result<...>>`.

Reference examples in this repo:

- `references/functional-core-imperative-shell-example.md`
- `Capable.Monads.Tests/TestScenarios.cs`
- `Capable.Monads/Result.cs`

## Outcome

Produce a design or implementation with these properties:

1. A pure core that encodes business rules and state transitions.
2. A shell that performs I/O and delegates decisions to the core.
3. Strongly-typed success and failure values instead of exceptions for expected business outcomes.
4. Specs that prove the core is pure and the shell short-circuits correctly.

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

### 4. Put orchestration in the imperative shell

The shell gathers inputs, invokes dependencies, creates the core context, and persists results.

Use query syntax when it improves readability across `Result` and `Task<Result<...>>`.
Use `ToAsync()` when moving a pure `Result` into an async flow.

```csharp
public sealed class CommandHandler
{
    private readonly ValidationService _validationService;
    private readonly AuthorizationService _authorizationService;
    private readonly EventStore _eventStore;
    private readonly OrderDecision _decision = new();

    public CommandHandler(
        ValidationService validationService,
        AuthorizationService authorizationService,
        EventStore eventStore)
    {
        _validationService = validationService;
        _authorizationService = authorizationService;
        _eventStore = eventStore;
    }

    public Task<Result<DomainEvent[], DomainError>> HandleAsync(Command command)
    {
        return
            from validatedCommand in _validationService.ValidateAsync(command)
            from authorized in _authorizationService.AuthorizeAsync(new User())
            from history in _eventStore.LoadEventsAsync(validatedCommand)
            from events in _decision.Execute(new CommandContext(validatedCommand, history, authorized)).ToAsync()
            from _ in _eventStore.PersistEventsAsync(events)
            select events;
    }
}
```

Guidelines:

- Inject shell dependencies explicitly.
- Keep shell services thin and replaceable.
- Let failure short-circuit naturally through `Result`; avoid manual branching unless it improves clarity.

### 5. Decide where to translate errors

Use this rule:

- Domain rule violations become domain failures in the core.
- Infrastructure or integration failures should be translated at the shell boundary before entering the core, or before returning from the shell.

Do not leak transport exceptions or vendor error shapes into core decision logic.

### 6. Spec the separation directly

Write separate specs for the core and the shell.

Core specs should prove:

- no mocks are needed
- business rules fail with strongly-typed domain errors
- success returns the expected domain events
- identical input yields identical output

Shell specs should prove:

- dependencies are orchestrated in the expected order
- failures short-circuit and prevent later work
- the shell delegates the actual decision to the core

Minimal checklist:

```text
[ ] Core entry point returns Result<TSuccess, TFailure>
[ ] Core code has no I/O or framework dependencies
[ ] Success values are domain outputs, not response DTOs
[ ] Failure values are explicit, typed, and meaningful
[ ] Shell owns async work and external dependencies
[ ] Composition uses Bind/Map/LINQ instead of nested branching where practical
[ ] Specs cover both pure decisions and shell short-circuiting
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
- The shell contains business rule branches that should live in the core.
- Failure cases are represented only as strings when a typed domain error would be clearer.
- Specs for business logic require mocks or async setup.

## Example Prompts

- "Create a functional core / imperative shell command handler with Capable.Monads for placing an order."
- "Refactor this service so business rules return Result<DomainEvent[], DomainError> and I/O stays in the shell."
- "Review this handler for functional core / imperative shell separation using Capable.Monads."
- "Add specs that prove the core is pure and the shell short-circuits on validation failure."

## Output Expectations

When using this skill, produce:

1. A proposed domain model.
2. The pure core API and implementation.
3. The shell orchestration code.
4. Specs for core purity and shell short-circuiting.
5. A short explanation of why each concern belongs in the core or shell.