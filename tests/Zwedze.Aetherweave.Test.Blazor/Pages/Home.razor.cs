using Microsoft.AspNetCore.Components;

namespace Zwedze.Aetherweave.Test.Blazor.Pages;

public partial class Home : ComponentBase
{
    [Inject] public required WeatherForecastClient Client { get; set; }

    internal string? ResponseString { get; private set; }

    private async Task GetWeather()
    {
        ResponseString = await Client.GetWeatherForecast();
    }
}
