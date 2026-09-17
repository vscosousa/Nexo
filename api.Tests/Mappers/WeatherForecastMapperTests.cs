using Nexo.Api.Domain.Models;
using Nexo.Api.Mappers;
using Xunit;

namespace Nexo.Api.Tests.Mappers;

public class WeatherForecastMapperTests
{
    [Fact]
    public void GivenForecastInCelsius_WhenMapped_ThenDtoCarriesConvertedFahrenheit()
    {
        var forecast = new WeatherForecast
        {
            Date = new DateOnly(2026, 1, 1),
            TemperatureC = 0,
            Summary = "Freezing",
        };

        var dto = WeatherForecastMapper.ToDto(forecast);

        Assert.Equal(32, dto.TemperatureF);
        Assert.Equal(forecast.Date, dto.Date);
        Assert.Equal(forecast.Summary, dto.Summary);
    }
}
