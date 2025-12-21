using JetBrains.Annotations;
using Zwedze.Aetherweave.Application.Errors;

namespace Zwedze.Aetherweave.Application.CQRS;

public abstract record ResponseWrapper<T>
{
    private ResponseWrapper()
    {
    }

    [UsedImplicitly]
    public static ResponseWrapper<T> Ok(T value)
    {
        return new Success(value);
    }

    [UsedImplicitly]
    public static ResponseWrapper<T> Fail(Error error)
    {
        return new Failure(error);
    }

    [UsedImplicitly]
    public sealed record Success(T Value) : ResponseWrapper<T>;

    [UsedImplicitly]
    public sealed record Failure(Error Error) : ResponseWrapper<T>;
}

public abstract record ResponseWrapper
{
    private ResponseWrapper()
    {
    }

    [UsedImplicitly]
    public static ResponseWrapper Ok()
    {
        return new Success();
    }

    [UsedImplicitly]
    public static ResponseWrapper Fail(Error error)
    {
        return new Failure(error);
    }

    public sealed record Success : ResponseWrapper;

    [UsedImplicitly]
    public sealed record Failure(Error Error) : ResponseWrapper;
}
