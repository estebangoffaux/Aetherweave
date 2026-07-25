namespace Zwedze.Aetherweave.Core.Configurations.Exceptions;

public sealed class ConfigurationInvalidException(string sectionName, IReadOnlyCollection<string> errors)
    : Exception(FormatMessage(sectionName, errors))
{
    private static string FormatMessage(string sectionName, IReadOnlyCollection<string> readOnlyCollection)
    {
        return $"Configuration section '{sectionName}' is invalid. Errors: {string.Join(", ", readOnlyCollection)}";
    }
}
