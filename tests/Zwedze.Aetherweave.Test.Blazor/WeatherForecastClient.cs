namespace Zwedze.Aetherweave.Test.Blazor;

public sealed class WeatherForecastClient(HttpClient client)
{
    public async Task<string> GetWeatherForecast()
    {
        return await client.GetStringAsync("weatherforecast");
    }
}
