---
name: create-imperative-shell-csharp
description: "Use when creating, refactoring, or reviewing an imperative shell around a pure functional core. Trigger phrases: imperative shell, command handler, application service, orchestration, Capable.Monads, Result pipeline, short-circuiting, ToAsync, integration boundaries."
---

# Create Imperative Shell

Use this skill when the goal is to orchestrate I/O and integration work while delegating business decisions to a pure functional core.

This repository's conventions:

- `Result<TSuccess, TFailure>` generic order is success first, failure second.
- The imperative shell orchestrates validation, authorization, loading, persistence, and other effects with LINQ query syntax over `Result` and `Task<Result<...>>`.
- The shell should call into a pure core for business decisions rather than encode business rules directly.

Reference examples in this repo:

- `references/imperative-shell-example.md`
- `../../references/result-api.md`

## Outcome

Produce an application boundary implementation with these properties:

1. All external effects are orchestrated in one readable pipeline.
2. Business-rule decisions are delegated to a pure core.
3. Failures short-circuit naturally through `Result`.
4. Integration and infrastructure failures are translated into stable error shapes.
5. Specs prove orchestration order and short-circuiting behavior.

## Workflow

### 1. Define shell responsibilities and boundaries

List every step in the use case and classify it:

- Effectful boundary work: validation, authorization, loading, persistence, messaging.
- Pure decision work: state transitions and business rules.

Only keep effectful work in the shell. Route pure decisions to a core object/function.

### 2. Inject dependencies explicitly

Model the shell as an application service or command handler with constructor-injected dependencies.

Typical dependencies:

- Validation service
- Authorization service
- Repository/event store
- External clients
- A pure decision object

Keep each dependency narrow so orchestration remains easy to test.

### 3. Compose the pipeline with Result

Use query syntax when it improves readability across mixed sync/async steps.
Use `ToAsync()` to lift pure `Result` values into async composition.

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

Guidelines:

- Prefer linear composition over nested conditionals.
- Let `Result` short-circuit failure paths.
- Keep shell methods focused on orchestration, not calculation.

### 4. Translate errors at boundaries

Use this rule:

- Domain rule violations should already be domain failures from the core.
- Infrastructure and integration failures should be translated at shell boundaries.

Do not leak transport exceptions or vendor-specific error models through your application contract.

### 5. Verify short-circuiting and ordering

Write shell specs that prove:

- Dependencies are called in expected order.
- A failure halts later work.
- The shell delegates decision logic to the core.

Minimal checklist:

```text
[ ] Shell dependencies are constructor-injected and easy to mock/fake
[ ] Pipeline is expressed with Result composition (LINQ/Bind/Map)
[ ] Pure business decisions are delegated to a core
[ ] Infrastructure failures are translated before returning
[ ] Tests cover success path, early failure path, and no-extra-work guarantees
```

## Decision Points

### When query syntax is worth it

Use query syntax when there are 3+ composed steps or mixed sync/async operations. For very short flows, direct `Bind`/`Map` may be clearer.

### When to keep an explicit branch

Keep an explicit `if` branch only when it improves readability for a special case; otherwise preserve a linear `Result` pipeline.

### When to split a large shell

Split when one handler orchestrates multiple unrelated integration concerns. Keep each shell entry point aligned to one use case.

## Review Criteria

Flag the design if any of these are true:

- Shell code contains business-rule calculations that should live in the core.
- Failure handling relies on exceptions for expected outcomes.
- A failed step still triggers downstream side effects.
- Infrastructure error details leak into domain/application contracts.
- Shell tests assert final values only and do not verify orchestration behavior.

## Example Prompts

- "Create an imperative shell command handler that orchestrates validation, authorization, load, decide, and persist using Capable.Monads."
- "Refactor this application service into a short-circuiting Result pipeline with clear boundary error translation."
- "Review this handler and identify shell orchestration bugs or mixed-in business logic."
- "Add tests proving persistence is not called when validation fails."

## Output Expectations

When using this skill, produce:

1. A shell dependency list and boundary map.
2. A composed orchestration pipeline.
3. Boundary error translation points.
4. Shell-focused specs for order and short-circuiting.
5. A concise rationale for why logic remains in shell vs core.