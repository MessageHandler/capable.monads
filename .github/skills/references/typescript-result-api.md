# Result<TSuccess, TFailure> (TypeScript)

**Package:** `@messagehandler/capable.monads`

Represents a discriminated union of a successful outcome or a failure outcome.
Exactly one case is present at any time.

Generic order follows success-first semantics: `Result<TSuccess, TFailure>`.

---

## Type Definition

```ts
export type Success<TSuccess> = {
  readonly type: "Success";
  readonly value: TSuccess;
};

export type Failure<TFailure> = {
  readonly type: "Failure";
  readonly error: TFailure;
};

export type Result<TSuccess, TFailure> = Success<TSuccess> | Failure<TFailure>;
```

---

## Constructors

### `success`

```ts
success<TSuccess, TFailure = never>(value: TSuccess): Result<TSuccess, TFailure>
```

Creates a successful result.

### `successAsync`

```ts
successAsync<TSuccess, TFailure = never>(value: TSuccess): Promise<Result<TSuccess, TFailure>>
```

Async constructor for convenience.

### `failure`

```ts
failure<TFailure, TSuccess = never>(error: TFailure): Result<TSuccess, TFailure>
```

Creates a failed result.

---

## Guards

### `isSuccess`

```ts
isSuccess<TSuccess, TFailure>(result: Result<TSuccess, TFailure>): result is Success<TSuccess>
```

Narrowing guard for success case.

### `isFailure`

```ts
isFailure<TSuccess, TFailure>(result: Result<TSuccess, TFailure>): result is Failure<TFailure>
```

Narrowing guard for failure case.

---

## Core Sync Operations

### `map`

```ts
map<TSuccess, TFailure, TNextSuccess>(
  result: Result<TSuccess, TFailure>,
  mapper: (value: TSuccess) => TNextSuccess,
): Result<TNextSuccess, TFailure>
```

Transforms success; preserves failure.

### `mapFailure`

```ts
mapFailure<TSuccess, TFailure, TNextFailure>(
  result: Result<TSuccess, TFailure>,
  mapper: (error: TFailure) => TNextFailure,
): Result<TSuccess, TNextFailure>
```

Transforms failure; preserves success.

### `bind`

```ts
bind<TSuccess, TFailure, TNextSuccess>(
  result: Result<TSuccess, TFailure>,
  binder: (value: TSuccess) => Result<TNextSuccess, TFailure>,
): Result<TNextSuccess, TFailure>
```

Chains fallible steps with short-circuiting.

### `fold`

```ts
fold<TSuccess, TFailure, TResult>(
  result: Result<TSuccess, TFailure>,
  onFailure: (error: TFailure) => TResult,
  onSuccess: (value: TSuccess) => TResult,
): TResult
```

Collapses `Result` to a single output.

---

## Core Async Operations

### `toAsync`

```ts
toAsync<TSuccess, TFailure>(result: Result<TSuccess, TFailure>): Promise<Result<TSuccess, TFailure>>
```

Lifts sync result into `Promise<Result<...>>`.

### `bindAsync`

```ts
bindAsync<TSuccess, TFailure, TNextSuccess>(
  result: Result<TSuccess, TFailure>,
  binder: (value: TSuccess) => Promise<Result<TNextSuccess, TFailure>>,
): Promise<Result<TNextSuccess, TFailure>>
```

Async chaining with short-circuiting.

### `mapAsync`

```ts
mapAsync<TSuccess, TFailure, TNextSuccess>(
  result: Result<TSuccess, TFailure>,
  mapper: (value: TSuccess) => Promise<TNextSuccess>,
): Promise<Result<TNextSuccess, TFailure>>
```

Async success mapping.

### `mapFailureAsync`

```ts
mapFailureAsync<TSuccess, TFailure, TNextFailure>(
  result: Result<TSuccess, TFailure>,
  mapper: (error: TFailure) => Promise<TNextFailure>,
): Promise<Result<TSuccess, TNextFailure>>
```

Async failure mapping.

### `foldAsync`

```ts
foldAsync<TSuccess, TFailure, TResult>(
  result: Result<TSuccess, TFailure>,
  onFailure: (error: TFailure) => Promise<TResult>,
  onSuccess: (value: TSuccess) => Promise<TResult>,
): Promise<TResult>
```

Async folding to one output.

---

## Composition Guidance

1. Use `map` when next step cannot fail.
2. Use `bind` when next step returns `Result`.
3. Use `toAsync` at boundary where async orchestration starts.
4. Use `mapFailure` to translate error shapes at boundaries.

There is no LINQ query syntax in TypeScript. Prefer explicit `bind`/`map` pipelines.
