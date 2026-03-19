---
name: implement-imperative-shell-csharp
description: "Use when creating, refactoring, or reviewing an imperative shell around a pure functional core in C#. Trigger phrases: imperative shell, command handler, application service, orchestration, Capable.Monads, Result pipeline, short-circuiting, ToAsync, integration boundaries."
---

## Package Requirement

> ⚠️ When implementing an imperative shell in the **messagehandler** repository, always install the **Capable.Monads** NuGet package in the project.
>
> For C#: `dotnet add package Capable.Monads --prerelease`

> **Before proceeding, read and apply the generic skill first:**
> [`../implement-imperative-shell/SKILL.md`](../implement-imperative-shell/SKILL.md)
>
> Then apply the C#-specific additions below.

# Implement Imperative Shell — C# Specifics

References for this repo:

- [`references/imperative-shell-example.md`](references/imperative-shell-example.md)
- [`../../references/result-api.md`](../../references/result-api.md)

## Shell Shape in C#

Model the shell as a `sealed class` command handler with constructor-injected dependencies.

```csharp
public sealed class CommandHandler
{
    private readonly ValidationService _validationService;
    private readonly AuthorizationService _authorizationService;
    private readonly EventStore _eventStore;
    private readonly OrderDecision _decision;

    public CommandHandler(
        ValidationService validationService,
        AuthorizationService authorizationService,
        EventStore eventStore,
        OrderDecision decision)
    {
        _validationService = validationService;
        _authorizationService = authorizationService;
        _eventStore = eventStore;
        _decision = decision;
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

## C# Conventions

- Use LINQ query syntax (`from … select`) for multi-step async pipelines — it keeps mixed sync/async composition linear and readable.
- Use `ToAsync()` to lift a synchronous `Result` from the core into the async pipeline.
- Return type is `Task<Result<TSuccess, TFailure>>`.
- `sealed class` for command handlers; constructor injection for all dependencies.
- For the pure core shape, see [`implement-functional-core-csharp`](../implement-functional-core-csharp/SKILL.md).