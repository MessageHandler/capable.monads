# Future Ideas

Potential extensions for the `Result<TSuccess, TFailure>` API, if this library grows beyond its current monad-focused scope.

- `Tap` / `TapAsync`: observe success values without changing the result.
- `Ensure`: enforce additional predicates over success values.
- `Recover` / `RecoverWith`: convert failures into fallback success paths.
- `FoldAsync`: asynchronously collapse a result into a final value.
- `Combine` / `Traverse` helpers: compose multiple results while keeping failure propagation explicit.