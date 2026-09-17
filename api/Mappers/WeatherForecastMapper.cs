using Nexo.Api.Domain.Models;
using Nexo.Api.Domain.Dtos;

namespace Nexo.Api.Mappers;

public static class WeatherForecastMapper
{
    public static WeatherForecastDto ToDto(WeatherForecast forecast) => new()
    {
        Date = forecast.Date,
        TemperatureC = forecast.TemperatureC,
        TemperatureF = forecast.TemperatureF,
        Summary = forecast.Summary
    };
}
