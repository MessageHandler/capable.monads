using FluentAssertions;

namespace Result
{
    public class ResultSemanticsTests
    {
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
        public void Fold_ThrowsOnNullDelegates()
        {
            var success = Result<int, string>.Success(10);

            var failureHandlerAct = () => success.Fold<int>(null!, _ => 0);
            var successHandlerAct = () => success.Fold<int>(_ => 0, null!);

            failureHandlerAct.Should().Throw<ArgumentNullException>();
            successHandlerAct.Should().Throw<ArgumentNullException>();
        }
    }
}
