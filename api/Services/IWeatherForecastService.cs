using Nexo.Api.Domain.Dtos;

namespace Nexo.Api.Services;

public interface IWeatherForecastService
{
    IEnumerable<WeatherForecastDto> GetForecast(int days);
}
