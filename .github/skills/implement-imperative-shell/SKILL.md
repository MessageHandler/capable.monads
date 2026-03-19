---
name: implement-imperative-shell
description: "Use when creating, refactoring, or reviewing an imperative shell around a pure functional core in any language. Trigger phrases: imperative shell, command handler, application service, orchestration, Result pipeline, short-circuiting, integration boundaries."
---

# Implement Imperative Shell

Use this skill when the goal is to orchestrate I/O and integration work while delegating business decisions to a pure functional core.

**If you know the target language, also read the language-specific supplement:**
- C#: [`implement-imperative-shell-csharp/SKILL.md`](../implement-imperative-shell-csharp/SKILL.md)
- TypeScript: [`implement-imperative-shell-typescript/SKILL.md`](../implement-imperative-shell-typescript/SKILL.md)

Repository conventions (all languages):

- `Result<TSuccess, TFailure>` generic order is success first, failure second.
- The shell orchestrates validation, authorization, loading, persistence, and other effects.
- The shell calls into a pure core for business decisions; it must not encode business rules directly.
- For the pure core itself, see the functional core skill.

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

- **Effectful boundary work:** validation, authorization, loading, persistence, messaging.
- **Pure decision work:** state transitions and business rules.

Only keep effectful work in the shell. Route pure decisions to a core function or object.

### 2. Inject dependencies explicitly

Model the shell as an application service or command handler with injected dependencies.

Typical dependencies:

- Validation service
- Authorization service
- Repository / event store
- External clients
- The pure decision object or function

Keep each dependency narrow so orchestration remains easy to test.

### 3. Compose the pipeline with Result

Build a linear pipeline where each step receives the output of the previous one. Let `Result` carry failures — don't check intermediate results manually.

Typical orchestration order:

```
validate(command)
  → authorize(actor)
  → load(history/state)
  → decide(core)          ← only pure step; lift into pipeline
  → persist(events)
  → return events
```

Guidelines:

- Prefer linear composition over nested conditionals.
- Let `Result` short-circuit failure paths automatically.
- Keep shell methods focused on orchestration, not calculation.

### 4. Translate errors at boundaries

- Domain rule violations should already arrive as domain failures from the core.
- Infrastructure and integration failures must be translated at shell boundaries into stable error shapes.

Do not leak transport exceptions or vendor-specific error models through your application contract.

### 5. Verify short-circuiting and ordering

Write shell specs that prove:

- Dependencies are called in expected order.
- A failure at any step halts all later work.
- The shell delegates decision logic to the core (not re-implementing it).

## Decision Points

### When to use pipeline/query syntax vs explicit chaining

Use pipeline or query syntax when there are 3+ composed steps or mixed sync/async operations. For very short flows, direct chaining may be clearer.

### When to keep an explicit branch

Keep an explicit branch only when it improves readability for a special case; otherwise preserve a linear `Result` pipeline.

### When to split a large shell

Split when one handler orchestrates multiple unrelated integration concerns. Keep each shell entry point aligned to one use case.

## Review Criteria

Flag the design if any of these are true:

- Shell code contains business-rule calculations that should live in the core.
- Failure handling relies on exceptions for expected outcomes.
- A failed step still triggers downstream side effects.
- Infrastructure error details leak into domain/application contracts.
- Shell tests assert final values only and do not verify orchestration behavior.

## Completion Checklist

```
[ ] Shell dependencies are explicitly injected and easy to mock/fake
[ ] Pipeline is expressed with Result composition (Bind/Map/query syntax)
[ ] Pure business decisions are delegated to a core
[ ] Infrastructure failures are translated before returning
[ ] Tests cover success path, early failure path, and no-extra-work guarantees
```

## Output Expectations

When using this skill, produce:

1. A shell dependency list and boundary map.
2. A composed orchestration pipeline.
3. Boundary error translation points.
4. Shell-focused specs for order and short-circuiting.
5. A concise rationale for why logic remains in the shell vs the core.
