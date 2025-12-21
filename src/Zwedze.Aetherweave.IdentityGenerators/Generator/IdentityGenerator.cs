using IdGen;
using Polly;
using Polly.Retry;
using Zwedze.Aetherweave.SharedKernel;

namespace Zwedze.Aetherweave.IdentityGenerators.Generator;

internal sealed class IdentityGenerator : IIdentityGenerator
{
    private readonly IdGenerator _idGenerator;
    private readonly RetryPolicy _retryPolicy;

    internal IdentityGenerator(IdGenerator idGenerator)
    {
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
        _retryPolicy = Policy.Handle<InvalidSystemClockException>().RetryForever();
    }

    /// <inheritdoc />
    public long GetNextUid()
    {
        var result = 0L;
        _retryPolicy.Execute(() => result = _idGenerator.CreateId());
        return result;
    }

    public Id<T> CreateId<T>()
    {
        return Id<T>.From(GetNextUid());
    }

    public Code<T> CreateCode<T>()
    {
        return Code<T>.From(Guid.CreateVersion7().ToString());
    }
}
