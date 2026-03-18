namespace Result
{
    public sealed class Result<TSuccess, TFailure>
    {
        private readonly TSuccess _success;
        private readonly TFailure _failure;

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;

        private Result(bool isSuccess, TSuccess success, TFailure failure)
        {
            this.IsSuccess = isSuccess;
            this._success = success;
            this._failure = failure;
        }

        public static implicit operator Result<TSuccess, TFailure>(TSuccess value) =>
            Success(value);

        public static implicit operator Result<TSuccess, TFailure>(TFailure value) =>
            Failure(value);

        public static Result<TSuccess, TFailure> Success(TSuccess value) =>
            new Result<TSuccess, TFailure>(true, value, default!);

        public static Result<TSuccess, TFailure> Failure(TFailure value) =>
            new Result<TSuccess, TFailure>(false, default!, value);

        public static Result<TSuccess, TFailure> From(TSuccess value) => Success(value);

        public Result<TNextSuccess, TFailure> Map<TNextSuccess>(Func<TSuccess, TNextSuccess> map)
        {
            ArgumentNullException.ThrowIfNull(map);

            return this.IsSuccess
                ? Result<TNextSuccess, TFailure>.Success(map(this._success))
                : Result<TNextSuccess, TFailure>.Failure(this._failure);
        }

        public Result<TNextSuccess, TFailure> Bind<TNextSuccess>(Func<TSuccess, Result<TNextSuccess, TFailure>> bind)
        {
            ArgumentNullException.ThrowIfNull(bind);

            return this.IsSuccess
                ? bind(this._success)
                : Result<TNextSuccess, TFailure>.Failure(this._failure);
        }

        public async Task<Result<TNextSuccess, TFailure>> BindAsync<TNextSuccess>(
            Func<TSuccess, Task<Result<TNextSuccess, TFailure>>> bind)
        {
            ArgumentNullException.ThrowIfNull(bind);

            return this.IsSuccess
                ? await bind(this._success).ConfigureAwait(false)
                : Result<TNextSuccess, TFailure>.Failure(this._failure);
        }

        public TResult Fold<TResult>(Func<TFailure, TResult> onFailure, Func<TSuccess, TResult> onSuccess)
        {
            ArgumentNullException.ThrowIfNull(onFailure);
            ArgumentNullException.ThrowIfNull(onSuccess);

            return this.IsSuccess
                ? onSuccess(this._success)
                : onFailure(this._failure);
        }
    }

    public static class ResultExtension
    {
        public static Result<TProjectedSuccess, TFailure> SelectMany<TIntermediateSuccess, TProjectedSuccess, TSuccess, TFailure>(
            this Result<TSuccess, TFailure> first,
            Func<TSuccess, Result<TIntermediateSuccess, TFailure>> second,
            Func<TSuccess, TIntermediateSuccess, TProjectedSuccess> project)
        {
            ArgumentNullException.ThrowIfNull(second);
            ArgumentNullException.ThrowIfNull(project);

            return first.Bind(a => second(a).Map(b => project(a, b)));
        }

        public static Result<TProjectedSuccess, TFailure> Select<TProjectedSuccess, TSuccess, TFailure>(
            this Result<TSuccess, TFailure> first,
            Func<TSuccess, TProjectedSuccess> map)
        {
            ArgumentNullException.ThrowIfNull(map);

            return first.Map(map);
        }
    }

    public static class TaskExtension
    {
        public static Task<Result<TSuccess, TFailure>> ToAsync<TSuccess, TFailure>(this Result<TSuccess, TFailure> result)
        {
            return Task.FromResult(result);
        }

        public static Task<Result<TSuccess, TFailure>> ToAsync<TSuccess, TFailure>(this TSuccess value)
        {
            return Task.FromResult(Result<TSuccess, TFailure>.From(value));
        }

        public static async Task<Result<TProjectedSuccess, TFailure>> Select<TProjectedSuccess, TSuccess, TFailure>(
            this Task<Result<TSuccess, TFailure>> first,
            Func<TSuccess, TProjectedSuccess> map)
        {
            return (await first.ConfigureAwait(false)).Map(map);
        }

        public static async Task<Result<TProjectedSuccess, TFailure>> SelectMany<TIntermediateSuccess, TProjectedSuccess, TSuccess, TFailure>(
            this Task<Result<TSuccess, TFailure>> first,
            Func<TSuccess, Task<Result<TIntermediateSuccess, TFailure>>> second,
            Func<TSuccess, TIntermediateSuccess, TProjectedSuccess> project)
        {
            ArgumentNullException.ThrowIfNull(second);
            ArgumentNullException.ThrowIfNull(project);

            return await (await first.ConfigureAwait(false))
                .BindAsync(async a =>
                    (await second(a).ConfigureAwait(false)).Map(b => project(a, b)))
                .ConfigureAwait(false);
        }

        public static async Task<Result<TProjectedSuccess, TFailure>> SelectMany<TIntermediateSuccess, TProjectedSuccess, TSuccess, TFailure>(
            this Result<TSuccess, TFailure> first,
            Func<TSuccess, Task<Result<TIntermediateSuccess, TFailure>>> second,
            Func<TSuccess, TIntermediateSuccess, TProjectedSuccess> project)
        {
            ArgumentNullException.ThrowIfNull(second);
            ArgumentNullException.ThrowIfNull(project);

            return await first
                .BindAsync(async a =>
                    (await second(a).ConfigureAwait(false)).Map(b => project(a, b)))
                .ConfigureAwait(false);
        }

        public static async Task<Result<TProjectedSuccess, TFailure>> SelectMany<TIntermediateSuccess, TProjectedSuccess, TSuccess, TFailure>(
            this Task<Result<TSuccess, TFailure>> first,
            Func<TSuccess, Result<TIntermediateSuccess, TFailure>> second,
            Func<TSuccess, TIntermediateSuccess, TProjectedSuccess> project)
        {
            ArgumentNullException.ThrowIfNull(second);
            ArgumentNullException.ThrowIfNull(project);

            return (await first.ConfigureAwait(false)).Bind(a => second(a).Map(b => project(a, b)));
        }
    }
}
