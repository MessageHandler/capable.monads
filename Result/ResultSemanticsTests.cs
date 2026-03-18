using FluentAssertions;

namespace Result
{
    public class ResultSemanticsTests
    {
        [Fact]
        public void Success_HasExpectedFlags()
        {
            Result<int, string> result = 42;

            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
        }

        [Fact]
        public void Failure_HasExpectedFlags()
        {
            Result<int, string> result = "boom";

            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
        }

        [Fact]
        public void Map_DoesNotInvokeMapper_OnFailure()
        {
            Result<int, string> failure = "bad input";
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
            Result<int, string> failure = "bad input";
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
            Result<int, string> failure = "bad input";
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
            Result<int, string> success = 10;

            var act = () => success.Map<int>(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Bind_ThrowsOnNullDelegate()
        {
            Result<int, string> success = 10;

            var act = () => success.Bind<int>(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task BindAsync_ThrowsOnNullDelegate()
        {
            Result<int, string> success = 10;

            var act = async () => await success.BindAsync<int>(null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public void Fold_ThrowsOnNullDelegates()
        {
            Result<int, string> success = 10;

            var failureHandlerAct = () => success.Fold<int>(null!, _ => 0);
            var successHandlerAct = () => success.Fold<int>(_ => 0, null!);

            failureHandlerAct.Should().Throw<ArgumentNullException>();
            successHandlerAct.Should().Throw<ArgumentNullException>();
        }
    }
}
