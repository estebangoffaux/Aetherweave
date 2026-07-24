using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zwedze.Aetherweave.Core.Configurations;
using Zwedze.Aetherweave.Security.Configuration;

namespace Zwedze.Aetherweave.Security;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAetherweaveJwtBearerAuthentication(this IServiceCollection services, IConfiguration configuration, string sectionName = "Aetherweave:Security:JwtBearer")
    {
        var options = ConfigurationLoader.GetOptions<AetherweaveJwtBearerOptions>(configuration, sectionName);
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.Authority = options.Authority;
                o.Audience = options.Audience;
                o.RequireHttpsMetadata = options.RequireHttpsMetadata;
            });
        return services;
    }
}
