using Microsoft.Extensions.Hosting;

namespace Zwedze.Aetherweave.Test.Console;

internal sealed class TestHostedService(WeatherForecastClient client) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var weatherForecast = await client.GetWeatherForecast();
        System.Console.WriteLine("Forecast received: " + weatherForecast);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
