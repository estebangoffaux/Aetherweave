using System.Security.Claims;
using Zwedze.Aetherweave.Security.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
// Configure jwt auth
builder
    .Services
    .AddAetherweaveJwtBearerAuthentication(builder.Configuration);

// Configure standard aspnet authorization
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

WeatherForecast[] GenerateForecast()
{
    return Enumerable
        .Range(1, 5)
        .Select(index =>
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]))
        .ToArray();
}

app
    .MapGet("/weatherforecast", GenerateForecast)
    .WithName("GetWeatherForecast");

app
    .MapGet("/weatherforecast/secure",
        (ClaimsPrincipal user) => new
        {
            Forecast = GenerateForecast(),
            Caller = user.Claims.Select(c => new { c.Type, c.Value }),
        })
    .WithName("GetSecureWeatherForecast")
    .RequireAuthorization();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
