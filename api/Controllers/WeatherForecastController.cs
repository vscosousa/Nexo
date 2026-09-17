using Microsoft.AspNetCore.Mvc;
using Nexo.Api.Domain.Dtos;
using Nexo.Api.Services;

namespace Nexo.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(IWeatherForecastService service) : ControllerBase
{
    /// <summary>Returns a 5-day weather forecast.</summary>
    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecastDto> Get()
    {
        return service.GetForecast(days: 5);
    }
}
