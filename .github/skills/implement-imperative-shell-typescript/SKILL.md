---
name: implement-imperative-shell-typescript
description: "Use when creating, refactoring, or reviewing a TypeScript imperative shell around a pure functional core with Capable.Monads Result pipelines, short-circuiting orchestration, async boundaries, and dependency-driven I/O. Trigger phrases: imperative shell typescript, command handler ts, application service ts, orchestration ts, result pipeline async ts."
---

## Package Requirement

> ⚠️ When implementing an imperative shell in the **messagehandler** repository, always install the **@messagehandler/capable.monads** npm package in the project.
>
> For TypeScript: `npm install @messagehandler/capable.monads@latest`

> **Before proceeding, read and apply the generic skill first:**
> [`../implement-imperative-shell/SKILL.md`](../implement-imperative-shell/SKILL.md)
>
> Then apply the TypeScript-specific additions below.

# Implement Imperative Shell — TypeScript Specifics

References for this repo:

- [`../../references/typescript-imperative-shell-example.md`](../../references/typescript-imperative-shell-example.md)
- [`../../references/typescript-result-api.md`](../../references/typescript-result-api.md)

## Shell Shape in TypeScript

No LINQ query syntax — compose steps with `bind`, `bindAsync`, `map`, `mapFailure`, and `toAsync` as explicit function calls.

Typical async pipeline:

```ts
async function handle(command: Command): Promise<Result<DomainEvent[], DomainError>> {
  const validated = await validateAsync(command);
  const authorized = await bindAsync(validated, () => authorizeAsync());
  const history = await bindAsync(authorized, auth => loadEventsAsync(auth));
  const events = await bindAsync(history, h => toAsync(decision.execute(buildContext(validated, h, authorized))));
  return bindAsync(events, evts => persistEventsAsync(evts));
}
```

## TypeScript Conventions

- Use `bindAsync` for each async step; use `toAsync` to lift the synchronous core result into the async chain.
- Functions are preferred over classes for handlers unless stateful configuration is needed.
- `mapFailure` is the idiomatic way to translate infrastructure errors into domain/application error shapes at boundaries.
- For the pure core shape, see [`implement-functional-core-typescript`](../implement-functional-core-typescript/SKILL.md).
