namespace Zwedze.Aetherweave.IdentityGenerators.Configuration;

/// <summary>
///     Options class that defines the options that should be used with the generator.
/// </summary>
public record UidGenerationOptions
{
    /// <summary>
    ///     Creates an instance of <see cref="UidGenerationOptions" />.
    /// </summary>
    internal UidGenerationOptions()
    {
    }

    /// <summary>
    ///     The strategy that should be used to generate the instance id.
    /// </summary>
    public InstanceIdType InstanceIdType { get; set; }

    /// <summary>
    ///     The epoch time that should be used.
    ///     Defaults to 2019/01/01.
    /// </summary>
    public DateTimeOffset EpochStart { get; set; } = new(
        2019,
        1,
        1,
        0,
        0,
        0,
        TimeSpan.Zero);
}
