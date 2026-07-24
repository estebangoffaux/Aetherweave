namespace Zwedze.Aetherweave.Core.Configurations.Exceptions;

public sealed class ConfigurationNotFoundException(string sectionName)
    : Exception(FormatMessage(sectionName))
{
    private static string FormatMessage(string sectionName)
    {
        return $"Configuration section '{sectionName}' not found. Ensure your appsettings contains the required configuration.";
    }
}
