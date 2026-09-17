using Nexo.Api.Domain.Dtos;

namespace Nexo.Api.Services;

public interface IWeatherForecastService
{
    /// <summary>Gets a forecast for the given number of days.</summary>
    IEnumerable<WeatherForecastDto> GetForecast(int days);
}
