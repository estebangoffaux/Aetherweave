using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Zwedze.Aetherweave.Security.Oidc;
using Zwedze.Aetherweave.Test.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAetherweaveOidcClientAuthentication(builder.Configuration);

builder.Services
    .AddAetherweaveOidcHttpClients(["https://localhost:7055"])
    .AddAetherweaveHttpClient<WeatherForecastClient, WeatherForecastClient>(builder.Configuration, "WeatherClient");

await builder.Build().RunAsync();
