using Nexo.Api.Domain.Dtos;
using Nexo.Api.Infrastructure.Repositories;
using Nexo.Api.Mappers;

namespace Nexo.Api.Services;

public class WeatherForecastService(IWeatherForecastRepository repository) : IWeatherForecastService
{
    public IEnumerable<WeatherForecastDto> GetForecast(int days)
    {
        return repository.GetForecast(days).Select(WeatherForecastMapper.ToDto);
    }
}
