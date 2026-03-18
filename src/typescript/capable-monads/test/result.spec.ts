import { expect } from "chai";
import {
  bind,
  bindAsync,
  failure,
  fold,
  isFailure,
  isSuccess,
  map,
  mapFailure,
  mapFailureAsync,
  success,
  type Result,
} from "../src/index.js";

const assertEquivalent = <TSuccess, TFailure>(
  actual: Result<TSuccess, TFailure>,
  expected: Result<TSuccess, TFailure>,
): void => {
  expect(actual.type).to.equal(expected.type);
  expect(fold(actual, (f) => f as unknown, (s) => s as unknown)).to.deep.equal(
    fold(expected, (f) => f as unknown, (s) => s as unknown),
  );
};

describe("Result semantics", () => {
  it("bind satisfies left identity", () => {
    const binder = (value: number): Result<number, string> => success(value + 1);

    const fromReturn = bind(success<number, string>(41), binder);
    const direct = binder(41);

    assertEquivalent(fromReturn, direct);
  });

  it("bind satisfies right identity for success", () => {
    const result = success<number, string>(42);

    const bound = bind(result, success<number, string>);

    assertEquivalent(bound, result);
  });

  it("bind satisfies right identity for failure", () => {
    const result = failure<string, number>("boom");

    const bound = bind(result, success<number, string>);

    assertEquivalent(bound, result);
  });

  it("bind satisfies associativity", () => {
    const first = (value: number): Result<number, string> => success(value + 1);
    const second = (value: number): Result<string, string> => success(`value:${value}`);

    const left = bind(bind(success<number, string>(41), first), second);
    const right = bind(success<number, string>(41), (value) => bind(first(value), second));

    assertEquivalent(left, right);
  });

  it("bind and map compose two successful steps", () => {
    const composed = bind(success<number, string>(20), (first) =>
      map(success<number, string>(22), (second) => first + second),
    );

    expect(fold(composed, () => -1, (value) => value)).to.equal(42);
  });

  it("success has expected flags", () => {
    const result = success<number, string>(42);

    expect(isSuccess(result)).to.equal(true);
    expect(isFailure(result)).to.equal(false);
  });

  it("failure has expected flags", () => {
    const result = failure<string, number>("boom");

    expect(isSuccess(result)).to.equal(false);
    expect(isFailure(result)).to.equal(true);
  });

  it("success stores value", () => {
    const result = success<number, string>(123);

    const value = fold(result, () => -1, (s) => s);

    expect(value).to.equal(123);
  });

  it("failure stores value", () => {
    const result = failure<string, number>("not valid");

    const value = fold(result, (f) => f, () => "ok");

    expect(value).to.equal("not valid");
  });

  it("same-type success and failure use explicit factories", () => {
    const ok = success<string, string>("ok");
    const err = failure<string, string>("error");

    expect(isSuccess(ok)).to.equal(true);
    expect(isFailure(err)).to.equal(true);
    expect(fold(ok, (f) => f, (s) => s)).to.equal("ok");
    expect(fold(err, (f) => f, (s) => s)).to.equal("error");
  });

  it("map does not invoke mapper on failure", () => {
    const err = failure<string, number>("bad input");
    let mapperCalled = false;

    const mapped = map(err, (value) => {
      mapperCalled = true;
      return value + 1;
    });

    expect(isFailure(mapped)).to.equal(true);
    expect(mapperCalled).to.equal(false);
  });

  it("bind does not invoke binder on failure", () => {
    const err = failure<string, number>("bad input");
    let binderCalled = false;

    const bound = bind(err, (value) => {
      binderCalled = true;
      return success<number, string>(value + 1);
    });

    expect(isFailure(bound)).to.equal(true);
    expect(binderCalled).to.equal(false);
  });

  it("bindAsync does not invoke binder on failure", async () => {
    const err = failure<string, number>("bad input");
    let binderCalled = false;

    const bound = await bindAsync(err, async (value) => {
      binderCalled = true;
      return success<number, string>(value + 1);
    });

    expect(isFailure(bound)).to.equal(true);
    expect(binderCalled).to.equal(false);
  });

  it("map throws on null mapper", () => {
    const ok = success<number, string>(10);

    expect(() => map(ok, undefined as unknown as (value: number) => number)).to.throw(TypeError);
  });

  it("bind throws on null binder", () => {
    const ok = success<number, string>(10);

    expect(() => bind(ok, undefined as unknown as (value: number) => Result<number, string>)).to.throw(TypeError);
  });

  it("bindAsync throws on null binder", async () => {
    const ok = success<number, string>(10);

    let threw = false;
    try {
      await bindAsync(ok, undefined as unknown as (value: number) => Promise<Result<number, string>>);
    } catch {
      threw = true;
    }

    expect(threw).to.equal(true);
  });

  it("mapFailure does not invoke mapper on success", () => {
    const ok = success<number, string>(42);
    let mapperCalled = false;

    const mapped = mapFailure(ok, (err) => {
      mapperCalled = true;
      return err.toUpperCase();
    });

    expect(isSuccess(mapped)).to.equal(true);
    expect(mapperCalled).to.equal(false);
    expect(fold(mapped, () => -1, (s) => s)).to.equal(42);
  });

  it("mapFailure transforms failure", () => {
    const err = failure<string, number>("error");

    const mapped = mapFailure(err, (f) => f.toUpperCase());

    expect(isFailure(mapped)).to.equal(true);
    expect(fold(mapped, (f) => f, () => "")).to.equal("ERROR");
  });

  it("mapFailureAsync transforms failure", async () => {
    const err = failure<string, number>("error");

    const mapped = await mapFailureAsync(err, async (f) => f.toUpperCase());

    expect(isFailure(mapped)).to.equal(true);
    expect(fold(mapped, (f) => f, () => "")).to.equal("ERROR");
  });

  it("mapFailure throws on null mapper", () => {
    const err = failure<string, number>("error");

    expect(() => mapFailure(err, undefined as unknown as (failureValue: string) => object)).to.throw(TypeError);
  });

  it("fold throws on null delegates", () => {
    const ok = success<number, string>(10);

    expect(() => fold(ok, undefined as unknown as (e: string) => number, () => 0)).to.throw(TypeError);
    expect(() => fold(ok, () => 0, undefined as unknown as (v: number) => number)).to.throw(TypeError);
  });
});
