namespace Zwedze.Aetherweave.IdentityGenerators.Configuration;

/// <summary>
///     Enum defining which strategy should be used while generating the instance id.
/// </summary>
public enum InstanceIdType
{
    /// <summary>
    ///     Undefined strategy.
    /// </summary>
    Undefined = 0,

    /// <summary>
    ///     Use the machine name.
    /// </summary>
    MachineName = 1,

    /// <summary>
    ///     Use the ip address.
    /// </summary>
    Ip = 2
}
