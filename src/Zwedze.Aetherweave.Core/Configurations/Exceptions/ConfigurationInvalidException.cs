namespace Zwedze.Aetherweave.Core.Configurations.Exceptions;

/// <summary>
///     Thrown by <see cref="ConfigurationLoader.GetOptions{T}" /> when a section cannot be bound or fails validation.
/// </summary>
public sealed class ConfigurationInvalidException(string sectionName, IReadOnlyCollection<string> errors)
    : Exception(FormatMessage(sectionName, errors))
{
    /// <summary>The section path that is invalid.</summary>
    public string SectionName { get; } = sectionName;

    /// <summary>Every validation error found in the section.</summary>
    public IReadOnlyCollection<string> Errors { get; } = errors;

    private static string FormatMessage(string sectionName, IReadOnlyCollection<string> errors)
    {
        return $"Configuration section '{sectionName}' is invalid. Errors: {string.Join(", ", errors)}";
    }
}
