using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zwedze.Aetherweave.Http;
using Zwedze.Aetherweave.Security.ClientCredentials;
using Zwedze.Aetherweave.Test.Console;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<TestHostedService>();

builder.Services.AddAetherweaveClientCredentialsAuthentication(builder.Configuration);

builder.Services
    .AddAetherweaveHttpClient<WeatherForecastClient, WeatherForecastClient>(builder.Configuration, "WeatherClient")
    .WithClientCredentialsAuthentication("test-console");

var app = builder.Build();
await app.RunAsync();
