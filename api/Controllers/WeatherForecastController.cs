using Microsoft.AspNetCore.Mvc;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Services;

namespace Nexo.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(IWeatherForecastService service) : ControllerBase
{
    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecastDto> Get()
    {
        return service.GetForecast(days: 5);
    }
}
