using JetBrains.Annotations;

namespace Zwedze.Aetherweave.FunctionalProgramming;

public abstract class Result<T>
{
    public abstract bool IsSuccess { get; }
    [UsedImplicitly]
    public bool IsFailure => !IsSuccess;

    [UsedImplicitly]
    public static Result<T> Success(T value)
    {
        return new SuccessResult<T>(value);
    }

    [UsedImplicitly]
    public static Result<T> Fail(Error error)
    {
        return new FailureResult<T>(error);
    }

    [UsedImplicitly]
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess)
        {
            _ = Match(v =>
                {
                    action(v);
                    return 0;
                },
                _ => 0);
        }
        return this;
    }

    [UsedImplicitly]
    public Result<T> OnFailure(Action<Error> action)
    {
        if (IsFailure)
        {
            _ = Match(_ => 0,
                e =>
                {
                    action(e);
                    return 0;
                });
        }
        return this;
    }

    [UsedImplicitly]
    public abstract TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure);

    [UsedImplicitly]
    public abstract Task<TResult> MatchAsync<TResult>(Func<T, Task<TResult>> onSuccess, Func<Error, Task<TResult>> onFailure);

    [UsedImplicitly]
    public abstract Result<TNext> Bind<TNext>(Func<T, Result<TNext>> next);

    [UsedImplicitly]
    public abstract Task<Result<TNext>> BindAsync<TNext>(Func<T, Task<Result<TNext>>> next);

    [UsedImplicitly]
    public abstract Result<TNext> Map<TNext>(Func<T, TNext> mapper);
}
