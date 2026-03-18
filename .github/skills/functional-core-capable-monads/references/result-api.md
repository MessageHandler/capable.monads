# Result&lt;TSuccess, TFailure&gt;

**Namespace:** `Capable.Monads`  
**Assembly:** `Capable.Monads`  
**Source:** `Capable.Monads/Result.cs`

Represents a discriminated union of a successful outcome or a failure outcome. Exactly one of the two cases is present at any given time.

Generic parameters follow success-first order: `Result<TSuccess, TFailure>`.

---

## Type Parameters

| Parameter | Description |
|-----------|-------------|
| `TSuccess` | The type of the value carried when the result is successful. |
| `TFailure` | The type of the value carried when the result is a failure. |

---

## Properties

### `IsSuccess`

```csharp
public bool IsSuccess { get; }
```

Returns `true` when the result represents a successful outcome. The success value is present and the failure value is absent.

---

### `IsFailure`

```csharp
public bool IsFailure { get; }
```

Returns `true` when the result represents a failed outcome. Equivalent to `!IsSuccess`.

---

## Static Factory Methods

### `Success`

```csharp
public static Result<TSuccess, TFailure> Success(TSuccess value)
```

Creates a successful result carrying `value`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `value` | `TSuccess` | The success value. |

**Returns:** A `Result<TSuccess, TFailure>` with `IsSuccess = true`.

---

### `SuccessAsync`

```csharp
public static Task<Result<TSuccess, TFailure>> SuccessAsync(TSuccess value)
```

Creates a successful result wrapped in a completed `Task`. Convenience shorthand for `Task.FromResult(Success(value))`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `value` | `TSuccess` | The success value. |

**Returns:** A `Task<Result<TSuccess, TFailure>>` that completes immediately with a successful result.

---

### `Failure`

```csharp
public static Result<TSuccess, TFailure> Failure(TFailure value)
```

Creates a failed result carrying `value`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `value` | `TFailure` | The failure value. |

**Returns:** A `Result<TSuccess, TFailure>` with `IsFailure = true`.

---

## Instance Methods

### `Map`

```csharp
public Result<TNextSuccess, TFailure> Map<TNextSuccess>(Func<TSuccess, TNextSuccess> map)
```

Transforms the success value. If the result is a failure, the failure propagates unchanged and `map` is never called.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `map` | `Func<TSuccess, TNextSuccess>` | Transformation applied to the success value. Must not be `null`. |

**Returns:** A new `Result<TNextSuccess, TFailure>` with the mapped success value, or the original failure.

**Throws:** `ArgumentNullException` when `map` is `null`.

**When to use:** When the next step transforms a value and cannot itself fail — that is, when the function always produces a `TNextSuccess` and never needs to return a `Result`.

---

### `MapFailure`

```csharp
public Result<TSuccess, TNextFailure> MapFailure<TNextFailure>(Func<TFailure, TNextFailure> map)
```

Transforms the failure value. If the result is a success, the success propagates unchanged and `map` is never called.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `map` | `Func<TFailure, TNextFailure>` | Transformation applied to the failure value. Must not be `null`. |

**Returns:** A new `Result<TSuccess, TNextFailure>` with the mapped failure value, or the original success.

**Throws:** `ArgumentNullException` when `map` is `null`.

**When to use:** When translating failure shapes at a boundary, for example converting an infrastructure error type to a domain error type before returning from the shell.

---

### `MapFailureAsync`

```csharp
public Task<Result<TSuccess, TNextFailure>> MapFailureAsync<TNextFailure>(
    Func<TFailure, Task<TNextFailure>> map)
```

Asynchronous version of `MapFailure`. Transforms the failure value using an async function. If the result is a success, it completes immediately without calling `map`.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `map` | `Func<TFailure, Task<TNextFailure>>` | Async transformation applied to the failure value. Must not be `null`. |

**Returns:** A `Task<Result<TSuccess, TNextFailure>>`.

**Throws:** `ArgumentNullException` when `map` is `null`.

---

### `Bind`

```csharp
public Result<TNextSuccess, TFailure> Bind<TNextSuccess>(
    Func<TSuccess, Result<TNextSuccess, TFailure>> bind)
```

Chains a fallible step. The function receives the current success value and returns a new `Result`. If the current result is a failure, `bind` is never called and the failure short-circuits to the output.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `bind` | `Func<TSuccess, Result<TNextSuccess, TFailure>>` | The next step. Must not be `null`. |

**Returns:** The `Result` produced by `bind`, or the original failure.

**Throws:** `ArgumentNullException` when `bind` is `null`.

**When to use:** When the next step can itself succeed or fail. Use `Map` when the next step always succeeds.

---

### `BindAsync`

```csharp
public Task<Result<TNextSuccess, TFailure>> BindAsync<TNextSuccess>(
    Func<TSuccess, Task<Result<TNextSuccess, TFailure>>> bind)
```

Asynchronous version of `Bind`. Chains a fallible async step. If the current result is a failure, `bind` is never called.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `bind` | `Func<TSuccess, Task<Result<TNextSuccess, TFailure>>>` | The async next step. Must not be `null`. |

**Returns:** A `Task<Result<TNextSuccess, TFailure>>`.

**Throws:** `ArgumentNullException` when `bind` is `null`.

---

### `Fold`

```csharp
public TResult Fold<TResult>(Func<TFailure, TResult> onFailure, Func<TSuccess, TResult> onSuccess)
```

Collapses the result into a single value by providing a handler for each case. Exactly one of the two handlers is called.

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `onFailure` | `Func<TFailure, TResult>` | Called when the result is a failure. Must not be `null`. |
| `onSuccess` | `Func<TSuccess, TResult>` | Called when the result is a success. Must not be `null`. |

**Returns:** The value produced by whichever handler was called.

**Throws:** `ArgumentNullException` when either delegate is `null`.

**When to use:** At the end of a pipeline when the caller needs to produce a single output, such as an HTTP response, a view model, or an assertion.

---

## Extension Methods

These are defined in `ResultExtension` and `TaskExtension` and enable LINQ query syntax over `Result` and `Task<Result<...>>`.

### `Select` (on `Result<TSuccess, TFailure>`)

```csharp
public static Result<TProjectedSuccess, TFailure> Select<TProjectedSuccess, TSuccess, TFailure>(
    this Result<TSuccess, TFailure> first,
    Func<TSuccess, TProjectedSuccess> map)
```

Enables `select` in LINQ query expressions over `Result`. Equivalent to calling `Map`.

---

### `SelectMany` (on `Result<TSuccess, TFailure>`)

```csharp
public static Result<TProjectedSuccess, TFailure> SelectMany<TIntermediateSuccess, TProjectedSuccess, TSuccess, TFailure>(
    this Result<TSuccess, TFailure> first,
    Func<TSuccess, Result<TIntermediateSuccess, TFailure>> second,
    Func<TSuccess, TIntermediateSuccess, TProjectedSuccess> project)
```

Enables `from ... from ... select` in LINQ query expressions over `Result`. Equivalent to calling `Bind` followed by `Map`.

---

### `ToAsync`

```csharp
public static Task<Result<TSuccess, TFailure>> ToAsync<TSuccess, TFailure>(
    this Result<TSuccess, TFailure> result)
```

Wraps a synchronous `Result` in a completed `Task` so it can participate in async LINQ query pipelines. This is the idiomatic way to call a pure, synchronous core method from inside an async shell pipeline.

```csharp
from events in _decision.Execute(context).ToAsync()
```

---

### `Select` (on `Task<Result<TSuccess, TFailure>>`)

```csharp
public static Task<Result<TProjectedSuccess, TFailure>> Select<TProjectedSuccess, TSuccess, TFailure>(
    this Task<Result<TSuccess, TFailure>> first,
    Func<TSuccess, TProjectedSuccess> map)
```

Enables `select` in LINQ query expressions over `Task<Result<...>>`. Awaits the task then applies `Map`.

---

### `SelectMany` overloads (on `Task<Result<TSuccess, TFailure>>`)

Three overloads allow mixing synchronous and asynchronous `Result` values in the same LINQ query expression:

| First | Second | Description |
|-------|--------|-------------|
| `Task<Result<...>>` | `Task<Result<...>>` | Both async. |
| `Task<Result<...>>` | `Result<...>` | Async first step, synchronous second. |
| `Result<...>` | `Task<Result<...>>` | Synchronous first step (e.g. pure core), async second (e.g. persistence). |

All overloads short-circuit on failure and never call the second function if the first result is a failure.

---

## Short-Circuiting Behaviour

Every method that accepts a delegate skips the delegate when the result is a failure and propagates the original failure value. This means a pipeline built with `Bind`, `Map`, and LINQ query syntax stops at the first failure without any explicit branching.

```csharp
// If ValidateAsync returns Failure, none of the remaining steps execute.
return
    from validatedCommand in _validationService.ValidateAsync(command)
    from authorized in _authorizationService.AuthorizeAsync(new User())
    from history in _eventStore.LoadEventsAsync(validatedCommand)
    from events in _decision.Execute(new CommandContext(validatedCommand, history, authorized)).ToAsync()
    from _ in _eventStore.PersistEventsAsync(events)
    select events;
```

---

## LINQ Query Syntax Summary

`Result<TSuccess, TFailure>` and `Task<Result<TSuccess, TFailure>>` both support full LINQ query syntax:

| Clause | Maps to |
|--------|---------|
| `from x in result` | `Bind` |
| `select x` | `Map` |
| `let x = expr` | local variable binding, no short-circuit |
| `from x in task` | async `Bind` via `SelectMany` overload |
| `.ToAsync()` | lifts synchronous `Result` into async pipeline |

---

## Related

- `references/functional-core-imperative-shell-example.md` — annotated walkthrough using this API
- `Capable.Monads.Tests/ResultSemanticsTests.cs` — monad law proofs and edge-case specs
