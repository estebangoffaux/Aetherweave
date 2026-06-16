using Zwedze.Aetherweave.SharedKernel;

namespace Zwedze.Aetherweave.IdentityGenerators;

public interface IIdentityGenerator
{
    /// <summary>
    ///     Generate a new uid.
    /// </summary>
    long GetNextUid();

    Id<T> CreateId<T>();
    Code<T> CreateCode<T>();
}
