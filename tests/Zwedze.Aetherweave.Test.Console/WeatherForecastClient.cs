namespace Zwedze.Aetherweave.Test.Console;

public sealed class WeatherForecastClient(HttpClient client)
{
    public async Task<string> GetWeatherForecast()
    {
        return await client.GetStringAsync("weatherforecast/secure");
    }
}
