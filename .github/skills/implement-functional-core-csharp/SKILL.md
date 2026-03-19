---
name: implement-functional-core-csharp
description: "Use when creating, refactoring, or reviewing a pure functional core in C#. Trigger phrases: functional core, pure domain logic, Capable.Monads, Result pipeline, domain events, domain errors, Bind, Map, deterministic rules."
---

## Package Requirement

> ⚠️ When implementing functional cores in the **messagehandler** repository, always install the **Capable.Monads** NuGet package in the project.
>
> For C#: `dotnet add package Capable.Monads --prerelease`

> **Before proceeding, read and apply the generic skill first:**
> [`../implement-functional-core/SKILL.md`](../implement-functional-core/SKILL.md)
>
> Then apply the C#-specific additions below.

# Implement Functional Core — C# Specifics

References for this repo:

- [`references/functional-core-example.md`](references/functional-core-example.md)
- [`../../references/result-api.md`](../../references/result-api.md)

## Domain Type Modeling in C#

Use `abstract record` hierarchies for domain errors and events. Use a `sealed class` for context objects.

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

## Composing Decisions in C#

Prefer LINQ query syntax (`from … select`) over chained method calls when composing multiple steps. Use `Map` and `Bind` for single-step transformations.

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
            return Result<Unit, DomainError>.Failure(
                new DomainError.BusinessRuleViolation("Orders must have positive quantity"));

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

## Async Boundary

Keep the core synchronous. At the shell boundary, use `ToAsync()` to lift a synchronous `Result` into an async pipeline. Never use `Task` inside the core unless the computation is genuinely asynchronous.

## C# Conventions

- Use `Unit` as the success value for steps that validate but produce no output.
- Avoid `Task` inside the core.
- `sealed class` for decision objects; `abstract record` for error/event hierarchies.
- Imperative shell lives in a separate class; see [`implement-imperative-shell-csharp`](../implement-imperative-shell-csharp/SKILL.md).