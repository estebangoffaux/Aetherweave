namespace Zwedze.Aetherweave.FunctionalProgramming;

internal sealed class FailureResult<T>(Error error) : Result<T>
{
    public override bool IsSuccess => false;

    public override TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<Error, TResult> onFailure)
    {
        return onFailure(error);
    }

    public override async Task<TResult> MatchAsync<TResult>(Func<T, Task<TResult>> onSuccess, Func<Error, Task<TResult>> onFailure)
    {
        return await onFailure(error);
    }

    public override Result<TNext> Bind<TNext>(Func<T, Result<TNext>> next)
    {
        return new FailureResult<TNext>(error);
    }

    public override Task<Result<TNext>> BindAsync<TNext>(Func<T, Task<Result<TNext>>> next)
    {
        return Task.FromResult<Result<TNext>>(new FailureResult<TNext>(error));
    }

    public override Result<TNext> Map<TNext>(Func<T, TNext> mapper)
    {
        return new FailureResult<TNext>(error);
    }
}
