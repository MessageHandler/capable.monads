import { expect } from "chai";
import {
  bind,
  bindAsync,
  failure,
  fold,
  isFailure,
  isSuccess,
  map,
  mapAsync,
  success,
  toAsync,
  type Result,
} from "../src/index.js";

describe("Result async composition", () => {
  it("toAsync lifts a result into Promise<Result>", async () => {
    const result = success<number, string>(42);
    const lifted = await toAsync(result);

    expect(isSuccess(lifted)).to.equal(true);
    expect(fold(lifted, () => -1, (v) => v)).to.equal(42);
  });

  it("mapAsync transforms success", async () => {
    const result = success<number, string>(41);
    const mapped = await mapAsync(result, async (value) => value + 1);

    expect(isSuccess(mapped)).to.equal(true);
    expect(fold(mapped, () => -1, (v) => v)).to.equal(42);
  });

  it("mapAsync does not invoke mapper on failure", async () => {
    const result = failure<string, number>("boom");
    let mapperCalled = false;

    const mapped = await mapAsync(result, async (value) => {
      mapperCalled = true;
      return value + 1;
    });

    expect(isFailure(mapped)).to.equal(true);
    expect(mapperCalled).to.equal(false);
  });

  it("bindAsync composes async continuation", async () => {
    const composed = await bindAsync(success<number, string>(20), async (v) => success<number, string>(v + 2));

    expect(fold(composed, () => -1, (v) => v)).to.equal(22);
  });

  it("short-circuits async pipeline on failure", async () => {
    let secondCalled = false;
    let thirdCalled = false;

    const validated: Result<number, string> = failure("validation failed");

    const authorized = bind(validated, (v) => {
      secondCalled = true;
      return success<number, string>(v + 1);
    });

    const pipeline = await bindAsync(authorized, async (v) => {
      thirdCalled = true;
      return success<number, string>(v + 1);
    });

    expect(isFailure(pipeline)).to.equal(true);
    expect(secondCalled).to.equal(false);
    expect(thirdCalled).to.equal(false);
  });

  it("supports mixed bind/map composition with toAsync", async () => {
    const sync = bind(success<number, string>(20), (v) => success<number, string>(v + 1));
    const mapped = map(sync, (v) => v + 1);
    const asyncResult = await toAsync(mapped);

    expect(fold(asyncResult, () => -1, (v) => v)).to.equal(22);
  });
});
