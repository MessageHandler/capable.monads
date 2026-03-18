using FluentAssertions;

namespace Capable.Monads
{
    public class ResultSemanticsTests
    {
        [Fact]
        public void Bind_SatisfiesLeftIdentity()
        {
            Func<int, Result<int, string>> bind = value =>
                Result<int, string>.Success(value + 1);

            var fromReturn = Result<int, string>.Success(41).Bind(bind);
            var direct = bind(41);

            AssertEquivalent(fromReturn, direct);
        }

        [Fact]
        public void Bind_SatisfiesRightIdentity_ForSuccess()
        {
            var result = Result<int, string>.Success(42);

            var bound = result.Bind(Result<int, string>.Success);

            AssertEquivalent(bound, result);
        }

        [Fact]
        public void Bind_SatisfiesRightIdentity_ForFailure()
        {
            var result = Result<int, string>.Failure("boom");

            var bound = result.Bind(Result<int, string>.Success);

            AssertEquivalent(bound, result);
        }

        [Fact]
        public void Bind_SatisfiesAssociativity()
        {
            Func<int, Result<int, string>> first = value =>
                Result<int, string>.Success(value + 1);
            Func<int, Result<string, string>> second = value =>
                Result<string, string>.Success($"value:{value}");

            var left = Result<int, string>.Success(41)
                .Bind(first)
                .Bind(second);

            var right = Result<int, string>.Success(41)
                .Bind(value => first(value).Bind(second));

            AssertEquivalent(left, right);
        }

        [Fact]
        public void LinqQuerySyntax_IsEquivalentToBindAndMap()
        {
            var query =
                from first in Result<int, string>.Success(20)
                from second in Result<int, string>.Success(22)
                select first + second;

            var fluent = Result<int, string>.Success(20)
                .Bind(first => Result<int, string>.Success(22)
                    .Map(second => first + second));

            AssertEquivalent(query, fluent);
        }

        [Fact]
        public void Success_HasExpectedFlags()
        {
            var result = Result<int, string>.Success(42);

            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
        }

        [Fact]
        public void Failure_HasExpectedFlags()
        {
            var result = Result<int, string>.Failure("boom");

            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
        }

        [Fact]
        public void Success_StoresValue()
        {
            var result = Result<int, string>.Success(123);

            var value = result.Fold(_ => -1, success => success);

            value.Should().Be(123);
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Failure_StoresValue()
        {
            var result = Result<int, string>.Failure("not valid");

            var value = result.Fold(failure => failure, _ => "ok");

            value.Should().Be("not valid");
            result.IsFailure.Should().BeTrue();
        }

        [Fact]
        public void SameTypeSuccessAndFailure_UsesExplicitFactories_ToAvoidAmbiguity()
        {
            var success = Result<string, string>.Success("ok");
            var failure = Result<string, string>.Failure("error");

            success.IsSuccess.Should().BeTrue();
            failure.IsFailure.Should().BeTrue();
            success.Fold(f => f, s => s).Should().Be("ok");
            failure.Fold(f => f, s => s).Should().Be("error");
        }

        [Fact]
        public void SameTypeSuccessAndFailure_RequiresExplicitFactories()
        {
            var success = Result<string, string>.Success("value");
            var failure = Result<string, string>.Failure("value");

            success.IsSuccess.Should().BeTrue();
            failure.IsFailure.Should().BeTrue();
        }

        [Fact]
        public void Map_DoesNotInvokeMapper_OnFailure()
        {
            var failure = Result<int, string>.Failure("bad input");
            var mapperCalled = false;

            var mapped = failure.Map(value =>
            {
                mapperCalled = true;
                return value + 1;
            });

            mapped.IsFailure.Should().BeTrue();
            mapperCalled.Should().BeFalse();
        }

        [Fact]
        public void Bind_DoesNotInvokeBinder_OnFailure()
        {
            var failure = Result<int, string>.Failure("bad input");
            var binderCalled = false;

            var bound = failure.Bind(value =>
            {
                binderCalled = true;
                return Result<int, string>.Success(value + 1);
            });

            bound.IsFailure.Should().BeTrue();
            binderCalled.Should().BeFalse();
        }

        [Fact]
        public async Task BindAsync_DoesNotInvokeBinder_OnFailure()
        {
            var failure = Result<int, string>.Failure("bad input");
            var binderCalled = false;

            var bound = await failure.BindAsync(value =>
            {
                binderCalled = true;
                return Task.FromResult(Result<int, string>.Success(value + 1));
            });

            bound.IsFailure.Should().BeTrue();
            binderCalled.Should().BeFalse();
        }

        [Fact]
        public void Map_ThrowsOnNullDelegate()
        {
            var success = Result<int, string>.Success(10);

            var act = () => success.Map<int>(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Bind_ThrowsOnNullDelegate()
        {
            var success = Result<int, string>.Success(10);

            var act = () => success.Bind<int>(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task BindAsync_ThrowsOnNullDelegate()
        {
            var success = Result<int, string>.Success(10);

            var act = async () => await success.BindAsync<int>(null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public void MapFailure_DoesNotInvokeMapper_OnSuccess()
        {
            var success = Result<int, string>.Success(42);
            var mapperCalled = false;

            var mapped = success.MapFailure(failure =>
            {
                mapperCalled = true;
                return failure.ToUpper();
            });

            mapped.IsSuccess.Should().BeTrue();
            mapperCalled.Should().BeFalse();
            mapped.Fold(_ => -1, s => s).Should().Be(42);
        }

        [Fact]
        public void MapFailure_TransformsFailure()
        {
            var failure = Result<int, string>.Failure("error");

            var mapped = failure.MapFailure(f => f.ToUpper());

            mapped.IsFailure.Should().BeTrue();
            mapped.Fold(f => f, _ => "").Should().Be("ERROR");
        }

        [Fact]
        public async Task MapFailureAsync_TransformsFailure()
        {
            var failure = Result<int, string>.Failure("error");

            var mapped = await failure.MapFailureAsync(async f =>
            {
                await Task.Delay(0);
                return f.ToUpper();
            });

            mapped.IsFailure.Should().BeTrue();
            mapped.Fold(f => f, _ => "").Should().Be("ERROR");
        }

        [Fact]
        public void MapFailure_ThrowsOnNullDelegate()
        {
            var failure = Result<int, string>.Failure("error");

            var act = () => failure.MapFailure<object>(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Fold_ThrowsOnNullDelegates()
        {
            var success = Result<int, string>.Success(10);

            var failureHandlerAct = () => success.Fold<int>(null!, _ => 0);
            var successHandlerAct = () => success.Fold<int>(_ => 0, null!);

            failureHandlerAct.Should().Throw<ArgumentNullException>();
            successHandlerAct.Should().Throw<ArgumentNullException>();
        }

        private static void AssertEquivalent<TSuccess, TFailure>(
            Result<TSuccess, TFailure> actual,
            Result<TSuccess, TFailure> expected)
        {
            actual.IsSuccess.Should().Be(expected.IsSuccess);
            actual.IsFailure.Should().Be(expected.IsFailure);

            actual.Fold(
                failure => (object?)failure,
                success => success)
                .Should()
                .Be(expected.Fold(
                    failure => (object?)failure,
                    success => success));
        }
    }
}
