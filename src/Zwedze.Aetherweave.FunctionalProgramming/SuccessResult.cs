namespace Zwedze.Aetherweave.FunctionalProgramming;

internal sealed class SuccessResult<T>(T value) : Result<T>
{
    public override bool IsSuccess => true;

    public override TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<Error, TResult> onFailure)
    {
        return onSuccess(value);
    }

    public override async Task<TResult> MatchAsync<TResult>(Func<T, Task<TResult>> onSuccess, Func<Error, Task<TResult>> onFailure)
    {
        return await onSuccess(value);
    }

    public override Result<TNext> Bind<TNext>(Func<T, Result<TNext>> next)
    {
        return next(value);
    }

    public override async Task<Result<TNext>> BindAsync<TNext>(Func<T, Task<Result<TNext>>> next)
    {
        return await next(value);
    }

    public override Result<TNext> Map<TNext>(Func<T, TNext> mapper)
    {
        return new SuccessResult<TNext>(mapper(value));
    }
}
