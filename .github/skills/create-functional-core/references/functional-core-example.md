# Functional Core Example

This note explains the functional-core portion of the functional core and imperative shell pattern.

## Purpose

The functional core demonstrates how business decisions stay pure and deterministic while returning explicit success and failure values.

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
3. Calling code does not need exception-driven control flow for business rules.

## Why the Core Stays Synchronous

The example keeps `OrderDecision.Execute` synchronous even though the shell around it is async.

That is the right default when the decision logic is pure. It keeps the core:

- easier to spec
- easier to reason about
- independent from asynchronous infrastructure concerns

If the method were made async without needing async effects, it would only add ceremony.

## Core Specs in the Example

Core specs prove:

- business rules are enforced in the core
- the core can be exercised without mocks
- failures are strongly typed domain errors

The specs are intentionally simple. Their role is to show where confidence should come from:

- pure business behavior is checked directly against the core

## Adaptation Guide (Core)

When reusing this example for a new feature, preserve the shape and replace the domain:

1. Rename command, validated command, and actor types.
2. Replace `DomainError` cases with your real business failures.
3. Replace `DomainEvent` cases with the outcomes your domain emits.
4. Update `CommandContext` to carry the exact inputs the decision needs.
5. Keep the decision method pure and synchronous unless there is a hard reason not to.
6. Write specs for core behavior separately from shell orchestration specs.

## Common Mistakes to Avoid (Core)

- Putting repository calls inside the decision object
- Returning transport DTOs from the core instead of domain results
- Using exceptions for normal business rule failures
- Hiding failures in booleans instead of typed failure values
- Making the core async when the only async work is outside the core