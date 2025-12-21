using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zwedze.Aetherweave.Data.Relational.Exceptions;
using Zwedze.Aetherweave.Data.Relational.UnitOfWork;

namespace Zwedze.Aetherweave.Data.Relational;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        [UsedImplicitly]
        public IServiceCollection AddAetherweaveData<TDbContext>(
            IConfiguration configuration,
            Action<DbContextOptionsBuilder, DataRelationalOptions> configure,
            string sectionName = "Aetherweave:DataRelational",
            bool addHealthCheck = true)
            where TDbContext : DbContext
        {
            CreateOptions();

            services.AddDbContext<TDbContext>((serviceProvider, dbContextOptions) =>
            {
                var opts = serviceProvider.GetRequiredService<IOptions<DataRelationalOptions>>();
                // Invoke the EF-provider specific configuration delegate
                configure(dbContextOptions, opts.Value);
                // Specify if detailed errors should be enabled
                dbContextOptions.EnableDetailedErrors(opts.Value.EnableDetailedErrors);
                // Specify if sensitive data logging should be enabled
                if (opts.Value.EnableSensitiveDataLogging)
                {
                    dbContextOptions.EnableSensitiveDataLogging();
                }
                // Specify NoTracking should be used as the default tracking strategy
                if (opts.Value.NoTrackingAsDefaultTrackingStrategy)
                {
                    dbContextOptions.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                }
            });

            // Allow the injection of DbContext instances
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());
            // Allow the injection of UnitOfWork instances
            services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();

            if (addHealthCheck)
            {
                services
                    .AddHealthChecks()
                    .AddDbContextCheck<TDbContext>(
                        $"{typeof(TDbContext).Name}",
                        tags: ["database"]);
            }

            return services;

            void CreateOptions()
            {
                var section = configuration.GetSection(sectionName);
                if (!section.Exists())
                {
                    throw new ConfigurationNotFoundException(sectionName);
                }

                // Register IOptions<DataRelationalOptions> in the DI container
                services
                    .AddOptions<DataRelationalOptions>()
                    .Bind(section)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
            }
        }
    }
}
