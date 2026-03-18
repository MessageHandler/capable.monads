export type Success<TSuccess> = {
  readonly type: "Success";
  readonly value: TSuccess;
};

export type Failure<TFailure> = {
  readonly type: "Failure";
  readonly error: TFailure;
};

export type Result<TSuccess, TFailure> = Success<TSuccess> | Failure<TFailure>;

function assertFunction(name: string, value: unknown): asserts value is Function {
  if (typeof value !== "function") {
    throw new TypeError(`${name} must be a function.`);
  }
}

export const success = <TSuccess, TFailure = never>(value: TSuccess): Result<TSuccess, TFailure> => ({
  type: "Success",
  value,
});

export const successAsync = async <TSuccess, TFailure = never>(
  value: TSuccess,
): Promise<Result<TSuccess, TFailure>> => success<TSuccess, TFailure>(value);

export const failure = <TFailure, TSuccess = never>(error: TFailure): Result<TSuccess, TFailure> => ({
  type: "Failure",
  error,
});

export const isSuccess = <TSuccess, TFailure>(result: Result<TSuccess, TFailure>): result is Success<TSuccess> =>
  result.type === "Success";

export const isFailure = <TSuccess, TFailure>(result: Result<TSuccess, TFailure>): result is Failure<TFailure> =>
  result.type === "Failure";

export const map = <TSuccess, TFailure, TNextSuccess>(
  result: Result<TSuccess, TFailure>,
  mapper: (value: TSuccess) => TNextSuccess,
): Result<TNextSuccess, TFailure> => {
  assertFunction("mapper", mapper);

  return isSuccess(result) ? success<TNextSuccess, TFailure>(mapper(result.value)) : failure<TFailure, TNextSuccess>(result.error);
};

export const mapFailure = <TSuccess, TFailure, TNextFailure>(
  result: Result<TSuccess, TFailure>,
  mapper: (error: TFailure) => TNextFailure,
): Result<TSuccess, TNextFailure> => {
  assertFunction("mapper", mapper);

  return isSuccess(result) ? success<TSuccess, TNextFailure>(result.value) : failure<TNextFailure, TSuccess>(mapper(result.error));
};

export const bind = <TSuccess, TFailure, TNextSuccess>(
  result: Result<TSuccess, TFailure>,
  binder: (value: TSuccess) => Result<TNextSuccess, TFailure>,
): Result<TNextSuccess, TFailure> => {
  assertFunction("binder", binder);

  return isSuccess(result) ? binder(result.value) : failure<TFailure, TNextSuccess>(result.error);
};

export const bindAsync = async <TSuccess, TFailure, TNextSuccess>(
  result: Result<TSuccess, TFailure>,
  binder: (value: TSuccess) => Promise<Result<TNextSuccess, TFailure>>,
): Promise<Result<TNextSuccess, TFailure>> => {
  assertFunction("binder", binder);

  return isSuccess(result) ? binder(result.value) : failure<TFailure, TNextSuccess>(result.error);
};

export const mapFailureAsync = async <TSuccess, TFailure, TNextFailure>(
  result: Result<TSuccess, TFailure>,
  mapper: (error: TFailure) => Promise<TNextFailure>,
): Promise<Result<TSuccess, TNextFailure>> => {
  assertFunction("mapper", mapper);

  return isSuccess(result)
    ? success<TSuccess, TNextFailure>(result.value)
    : failure<TNextFailure, TSuccess>(await mapper(result.error));
};

export const fold = <TSuccess, TFailure, TResult>(
  result: Result<TSuccess, TFailure>,
  onFailure: (error: TFailure) => TResult,
  onSuccess: (value: TSuccess) => TResult,
): TResult => {
  assertFunction("onFailure", onFailure);
  assertFunction("onSuccess", onSuccess);

  return isSuccess(result) ? onSuccess(result.value) : onFailure(result.error);
};
