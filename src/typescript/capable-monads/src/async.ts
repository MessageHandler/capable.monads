import { failure, isSuccess, type Result } from "./result.js";

function assertFunction(name: string, value: unknown): asserts value is Function {
  if (typeof value !== "function") {
    throw new TypeError(`${name} must be a function.`);
  }
}

export const toAsync = async <TSuccess, TFailure>(
  result: Result<TSuccess, TFailure>,
): Promise<Result<TSuccess, TFailure>> => result;

export const mapAsync = async <TSuccess, TFailure, TNextSuccess>(
  result: Result<TSuccess, TFailure>,
  mapper: (value: TSuccess) => Promise<TNextSuccess>,
): Promise<Result<TNextSuccess, TFailure>> => {
  assertFunction("mapper", mapper);

  return isSuccess(result)
    ? { type: "Success", value: await mapper(result.value) }
    : failure<TFailure, TNextSuccess>(result.error);
};

export const foldAsync = async <TSuccess, TFailure, TResult>(
  result: Result<TSuccess, TFailure>,
  onFailure: (error: TFailure) => Promise<TResult>,
  onSuccess: (value: TSuccess) => Promise<TResult>,
): Promise<TResult> => {
  assertFunction("onFailure", onFailure);
  assertFunction("onSuccess", onSuccess);

  return isSuccess(result) ? onSuccess(result.value) : onFailure(result.error);
};
