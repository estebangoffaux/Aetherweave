namespace Zwedze.Aetherweave.Core.Configurations.Exceptions;

/// <summary>
///     Thrown by <see cref="ConfigurationLoader" /> when the requested section does not exist in the configuration.
/// </summary>
/// <param name="sectionName">The section path that was not found.</param>
public sealed class ConfigurationNotFoundException(string sectionName)
    : Exception(FormatMessage(sectionName))
{
    /// <summary>The section path that was not found.</summary>
    public string SectionName { get; } = sectionName;

    private static string FormatMessage(string sectionName)
    {
        return $"Configuration section '{sectionName}' not found. Ensure your appsettings contains the required configuration.";
    }
}
