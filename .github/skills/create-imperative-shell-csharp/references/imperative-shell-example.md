# Imperative Shell Example

This note explains the imperative-shell portion of the functional core and imperative shell pattern.

## Purpose

The imperative shell demonstrates orchestration of effects around a pure decision core:

- a command comes in from the outside world
- the shell gathers the data the core needs
- the core makes a business decision
- the shell persists the resulting domain events

The key point is separation of concerns.

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

## Short-Circuiting Behavior

Because every shell step returns `Result<...>` or `Task<Result<...>>`, the pipeline stops on the first failure.

That means:

- if validation fails, authorization does not run
- if authorization fails, event loading does not run
- if loading fails, the core does not run
- if the core fails, persistence does not run

This is one of the main reasons the pattern works well with Capable.Monads.

## Shell Specs in the Example

Shell specs prove:

- dependencies can be wired explicitly
- orchestration works end to end
- failures short-circuit correctly

The specs are intentionally simple. Their role is to show where confidence should come from:

- effect sequencing is checked through the shell

## Adaptation Guide (Shell)

When reusing this example for a new feature, preserve the orchestration shape:

1. Keep external calls in the shell.
2. Delegate business decisions to a pure decision object.
3. Keep the pipeline linear with Result composition where practical.
4. Ensure failures short-circuit before downstream side effects.
5. Write shell specs that verify ordering and no-extra-work on failure.

## Common Mistakes to Avoid (Shell)

- Putting business-rule calculations in `CommandHandler`
- Catching and swallowing expected failures instead of returning typed errors
- Persisting side effects after earlier step failures
- Mixing shell validation and domain validation without a clear boundary