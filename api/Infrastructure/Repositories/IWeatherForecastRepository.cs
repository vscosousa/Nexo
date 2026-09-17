using Nexo.Api.Domain.Models;

namespace Nexo.Api.Infrastructure.Repositories;

public interface IWeatherForecastRepository
{
    /// <summary>Gets a forecast for the given number of days.</summary>
    IEnumerable<WeatherForecast> GetForecast(int days);
}
