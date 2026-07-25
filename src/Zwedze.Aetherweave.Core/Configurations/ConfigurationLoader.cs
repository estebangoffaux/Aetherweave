using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zwedze.Aetherweave.Core.Configurations.Exceptions;

namespace Zwedze.Aetherweave.Core.Configurations;

public static class ConfigurationLoader
{
    [UsedImplicitly]
    public static void RegisterOptions<T>(IServiceCollection services, IConfiguration configuration, string sectionName, string? optionName = null)
        where T : class
    {
        var section = GetSection(configuration, sectionName);

        services
            .AddOptions<T>(optionName)
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    [UsedImplicitly]
    public static void RegisterOptions<T>(IServiceCollection services, IEnumerable<IConfigurationSection> sections)
        where T : class
    {
        foreach (var section in sections)
        {
            services
                .AddOptions<T>(section.Key)
                .Bind(section)
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }
    }

    [UsedImplicitly]
    public static T GetOptions<T>(IConfiguration configuration, string sectionName)
        where T : class
    {
        var section = GetSection(configuration, sectionName);
        var options = section.Get<T>()!;

        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(options, new ValidationContext(options), results, true))
        {
            return options;
        }

        var errors = results
            .Select(x => x.ErrorMessage)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();
        throw new ConfigurationInvalidException(sectionName, errors);
    }

    private static IConfigurationSection GetSection(IConfiguration configuration, string sectionName)
    {
        var section = configuration.GetSection(sectionName);
        return section.Exists()
            ? section
            : throw new ConfigurationNotFoundException(sectionName);
    }
}
