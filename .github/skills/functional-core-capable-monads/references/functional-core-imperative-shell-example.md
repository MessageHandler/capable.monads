# Functional Core / Imperative Shell Example

This note explains the example in `Capable.Monads.Tests/FunctionalCoreImperativeShellExample.cs` and shows how it maps to the functional core / imperative shell pattern used in this repository.

## Purpose

The example demonstrates a simple rule:

- a command comes in from the outside world
- the shell gathers the data the core needs
- the core makes a business decision
- the shell persists the resulting domain events

The important part is not the order domain itself. The important part is the separation of concerns.

## High-Level Structure

The file has four layers:

1. Domain language: `DomainError`, `DomainEvent`, `CommandContext`
2. Functional core: `OrderDecision`
3. Imperative shell: `CommandHandler` and shell services
4. Specs: examples that prove the separation works

## Domain Language

The example starts by defining explicit domain types.

`DomainError`

- models expected business failures
- keeps error handling in business language instead of strings or exceptions

`DomainEvent`

- models what the business decided happened
- gives the core a stable success shape to return

`CommandContext`

- packages the inputs the core needs
- avoids long parameter lists
- makes the core API easier to read and evolve

This is a good default when the core needs validated input, actor information, and current state history together.

## Functional Core

The functional core is `OrderDecision`.

Its main method is:

```csharp
public Result<DomainEvent[], DomainError> Execute(CommandContext context)
```

That signature tells you nearly everything:

- the method is synchronous because the decision itself is pure
- success returns domain events
- failure returns domain errors
- the method does not throw for expected business outcomes

### Why `Execute` is in the core

`Execute` only decides what should happen. It does not:

- read from a database
- call an API
- log
- allocate identifiers from infrastructure
- save anything

It receives everything it needs as input and returns everything it decided as output.

### Internal Core Steps

The method composes two pure steps:

```csharp
return from _ in ValidateBusinessRules(context)
       from events in ComputeStateChanges(context)
       select events;
```

`ValidateBusinessRules`

- enforces domain invariants
- returns `Failure` when a rule is violated
- returns `Success(Unit)` when validation passes

`ComputeStateChanges`

- calculates the domain events to emit
- stays deterministic because it only depends on `context`

This split is useful because validation and state transition logic often evolve independently.

### Why `Result` matters here

The `Result<TSuccess, TFailure>` type gives the core three useful properties:

1. Expected failures are explicit in the method signature.
2. Composition short-circuits automatically.
3. The calling shell does not need exception-driven control flow for business rules.

## Imperative Shell

The shell is `CommandHandler`.

Its job is not to make business decisions. Its job is to orchestrate effects around the core.

The example shell does five things:

1. validate the incoming command
2. authorize the actor
3. load prior events
4. invoke the pure core
5. persist emitted events

That flow appears directly in the LINQ query:

```csharp
return await
    (from validatedCommand in this._validationService.ValidateAsync(command)
     from authorized in this._authorizationService.AuthorizeAsync(new User())
     from history in this._eventStore.LoadEventsAsync(validatedCommand)
     from events in this._decision.Execute(new CommandContext(validatedCommand, history, authorized)).ToAsync()
     from _ in this._eventStore.PersistEventsAsync(events)
     select events);
```

### Why each step is in the shell

`ValidateAsync`

- may involve I/O or cross-system checks
- prepares input for the core

`AuthorizeAsync`

- is usually integration-heavy
- belongs at the boundary even when conceptually important to the business

`LoadEventsAsync`

- reads external state
- provides the current history the core needs to decide correctly

`Execute(...).ToAsync()`

- calls the pure decision logic
- `ToAsync()` adapts a synchronous `Result` into the async pipeline

`PersistEventsAsync`

- writes to external storage
- stays outside the core because persistence is an effect, not a decision

## Why the Core Stays Synchronous

The example keeps `OrderDecision.Execute` synchronous even though the shell is async.

That is the right default when the decision logic is pure. It keeps the core:

- easier to spec
- easier to reason about
- independent from asynchronous infrastructure concerns

If the method were made async without needing async effects, it would only add ceremony.

## Short-Circuiting Behavior

Because every shell step returns `Result<...>` or `Task<Result<...>>`, the pipeline stops on the first failure.

That means:

- if validation fails, authorization does not run
- if authorization fails, event loading does not run
- if loading fails, the core does not run
- if the core fails, persistence does not run

This is one of the main reasons the pattern works well with Capable.Monads.

## Specs in the Example

The example includes specs for both sides of the boundary.

Core specs prove:

- business rules are enforced in the core
- the core can be exercised without mocks
- failures are strongly typed domain errors

Shell specs prove:

- dependencies can be wired explicitly
- orchestration works end to end
- failures short-circuit correctly

The specs are intentionally simple. Their role is to show where confidence should come from:

- pure business behavior is checked directly against the core
- effect sequencing is checked through the shell

## Adaptation Guide

When reusing this example for a new feature, preserve the shape and replace the domain:

1. Rename the command, validated command, and actor types.
2. Replace `DomainError` cases with your real business failures.
3. Replace `DomainEvent` cases with the outcomes your domain emits.
4. Update `CommandContext` to carry the exact inputs the decision needs.
5. Keep the decision method pure and synchronous unless there is a hard reason not to.
6. Keep external calls in the shell.
7. Write separate specs for core behavior and shell orchestration.

## Common Mistakes to Avoid

- Putting repository calls inside the decision object
- Returning transport DTOs from the core instead of domain results
- Using exceptions for normal business rule failures
- Hiding failures in booleans instead of typed failure values
- Making the core async when the only async work is outside the core
- Mixing shell validation and domain validation without a clear boundary

## Related Source Files

- `Capable.Monads.Tests/FunctionalCoreImperativeShellExample.cs`
- `Capable.Monads.Tests/TestScenarios.cs`
- `Capable.Monads/Result.cs`