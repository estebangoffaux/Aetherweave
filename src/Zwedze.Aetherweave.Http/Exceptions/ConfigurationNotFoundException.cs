namespace Zwedze.Aetherweave.Http.Exceptions;

public sealed class ConfigurationNotFoundException(string sectionName)
    : AetherweaveHttpException(FormatMessage(sectionName))
{
    private static string FormatMessage(string sectionName)
    {
        return $"Configuration section '{sectionName}' not found. Ensure your appsettings.json contains the required configuration.";
    }
}
