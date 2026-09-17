using Nexo.Api.Domain.Models;

namespace Nexo.Api.Infrastructure.Repositories;

public interface IWeatherForecastRepository
{
    IEnumerable<WeatherForecast> GetForecast(int days);
}
