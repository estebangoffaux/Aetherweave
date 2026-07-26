using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zwedze.Aetherweave.Security.Oidc;
using Zwedze.Aetherweave.Test.Console;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<TestHostedService>();

builder.Services.AddAetherweaveOidcClientAuthentication(builder.Configuration);

builder.Services
    .AddAetherweaveOidcHttpClients(["https://localhost:7055"])
    .AddAetherweaveHttpClient<WeatherForecastClient, WeatherForecastClient>(builder.Configuration, "WeatherClient");


var app = builder.Build();
await app.RunAsync();
