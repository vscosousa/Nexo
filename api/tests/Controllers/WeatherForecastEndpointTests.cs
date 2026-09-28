using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Nexo.Api.Domain.Dtos;
using Xunit;

namespace Nexo.Api.Tests.Controllers;

public class WeatherForecastEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GivenTheApiIsRunning_WhenRequestingWeatherForecast_ThenItReturnsFiveDays()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/WeatherForecast");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var forecasts = await response.Content.ReadFromJsonAsync<List<WeatherForecastDto>>();
        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Count);
    }
}
