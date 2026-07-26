using Microsoft.Extensions.Hosting;

namespace Zwedze.Aetherweave.Test.Console;

internal sealed class TestHostedService(WeatherForecastClient client, IHostApplicationLifetime lifetime) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var weatherForecast = await client.GetWeatherForecast();
        System.Console.WriteLine("Authenticated as service, secure forecast received: " + weatherForecast);

        lifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
