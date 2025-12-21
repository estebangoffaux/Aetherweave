using IdGen;

namespace Zwedze.Aetherweave.IdentityGenerators.Generator;

/// <summary>
///     Factory that creates an instance of <see cref="ITimeSource" />.
/// </summary>
internal interface ITimeSourceFactory
{
    /// <summary>
    ///     Creates an instance of <see cref="ITimeSource" />.
    /// </summary>
    /// <param name="epoch">The epoch that should be used.</param>
    /// <returns>The created <see cref="ITimeSource" />.</returns>
    ITimeSource Create(DateTimeOffset epoch);
}

/// <inheritdoc />
internal sealed class TimeSourceFactory : ITimeSourceFactory
{
    /// <inheritdoc />
    public ITimeSource Create(DateTimeOffset epoch)
    {
        if (epoch == default)
        {
            throw new ArgumentNullException(nameof(epoch));
        }

        return new DefaultTimeSource(epoch);
    }
}
