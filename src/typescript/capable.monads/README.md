# @messagehandler/capable.monads

A TypeScript `Result<TSuccess, TFailure>` implementation for functional core / imperative shell architecture and railroad-style composition.

## Install from GitHub Packages (private)

```bash
npm config set @messagehandler:registry https://npm.pkg.github.com
npm config set //npm.pkg.github.com/:_authToken \"<YOUR_TOKEN_WITH_read:packages>\"
npm install @messagehandler/capable.monads
```

## Quick Start

```ts
import { bind, failure, fold, map, success, type Result } from "@messagehandler/capable.monads";

type DomainError = { type: "BusinessRuleViolation"; rule: string };

const validatePositive = (value: number): Result<number, DomainError> =>
  value > 0 ? success(value) : failure({ type: "BusinessRuleViolation", rule: "Must be positive" });

const program = bind(validatePositive(41), (v) => success(v + 1));

const output = fold(program, (err) => err.rule, (v) => `ok:${v}`);
```

## Publish to GitHub Packages

```bash
npm run test
NODE_AUTH_TOKEN=<TOKEN_WITH_write:packages> npm run publish:github
```
