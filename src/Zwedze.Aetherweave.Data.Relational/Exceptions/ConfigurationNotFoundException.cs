namespace Zwedze.Aetherweave.Data.Relational.Exceptions;

public sealed class ConfigurationNotFoundException(string sectionName) : AetherweaveDataException(
    $"Configuration section '{sectionName}' not found. " +
    $"Ensure your appsettings.json contains the required configuration.");
