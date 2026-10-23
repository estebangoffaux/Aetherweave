using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zwedze.Aetherweave.Core.Configurations.Exceptions;

namespace Zwedze.Aetherweave.Core.Configurations;

/// <summary>
///     Binds configuration sections to strongly-typed options and validates them with data annotations
///     (and <see cref="IValidatableObject" />). Only top-level properties are validated: nested objects are not.
/// </summary>
public static class ConfigurationLoader
{
    /// <summary>
    ///     Registers <typeparamref name="TOption" /> in DI, bound to <paramref name="sectionName" /> and validated when the host starts.
    /// </summary>
    /// <typeparam name="TOption">The option type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to read from.</param>
    /// <param name="sectionName">The section path, e.g. <c>Aetherweave:HttpClients:OrderService</c>.</param>
    /// <param name="optionName">The named-option name; <c>null</c> registers the default (unnamed) option.</param>
    /// <exception cref="ConfigurationNotFoundException">The section does not exist (thrown immediately).</exception>
    /// <remarks>
    ///     Invalid values throw <see cref="OptionsValidationException" /> when the host starts
    ///     (or on first resolution when there is no host), never <see cref="ConfigurationInvalidException" />.
    /// </remarks>
    [UsedImplicitly]
    public static void RegisterOptions<TOption>(IServiceCollection services, IConfiguration configuration, string sectionName, string? optionName = null)
        where TOption : class
    {
        var section = GetSection(configuration, sectionName);

        services
            .AddOptions<TOption>(optionName)
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    /// <summary>
    ///     Registers one named <typeparamref name="TOption" /> per section, named after the section key and validated when the host starts.
    /// </summary>
    /// <typeparam name="TOption">The option type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="sections">The sections to register, typically <c>configuration.GetSection(parent).GetChildren()</c>.</param>
    /// <remarks>
    ///     An empty <paramref name="sections" /> registers nothing and does not throw.
    ///     Option names are case-sensitive: resolve them with the exact section key.
    ///     Invalid values throw <see cref="OptionsValidationException" /> when the host starts.
    /// </remarks>
    [UsedImplicitly]
    public static void RegisterOptions<TOption>(IServiceCollection services, IEnumerable<IConfigurationSection> sections)
        where TOption : class
    {
        foreach (var section in sections)
        {
            services
                .AddOptions<TOption>(section.Key)
                .Bind(section)
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }
    }

    /// <summary>
    ///     Binds <paramref name="sectionName" /> to <typeparamref name="TOption" /> and validates it immediately, outside DI.
    /// </summary>
    /// <typeparam name="TOption">The option type.</typeparam>
    /// <param name="configuration">The configuration to read from.</param>
    /// <param name="sectionName">The section path, e.g. <c>Aetherweave:Security:JwtBearer</c>.</param>
    /// <returns>The bound and validated options.</returns>
    /// <exception cref="ConfigurationNotFoundException">The section does not exist.</exception>
    /// <exception cref="ConfigurationInvalidException">The section cannot be bound to <typeparamref name="TOption" />, or validation fails.</exception>
    [UsedImplicitly]
    public static TOption GetOptions<TOption>(IConfiguration configuration, string sectionName)
        where TOption : class
    {
        var section = GetSection(configuration, sectionName);
        var options = section.Get<TOption>()
            ?? throw new ConfigurationInvalidException(sectionName, [$"The section cannot be bound to {typeof(TOption).Name}."]);

        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(options, new ValidationContext(options), results, true))
        {
            return options;
        }

        var errors = results
            .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage)
                ? $"Invalid value for {string.Join(", ", x.MemberNames)}."
                : x.ErrorMessage)
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
