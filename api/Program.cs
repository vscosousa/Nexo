using Microsoft.EntityFrameworkCore;
using Nexo.Api.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Nexo.Api.Infrastructure.Repositories;
using Nexo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<NexoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("NexoDb")));

builder.Services.AddScoped<IWeatherForecastRepository, WeatherForecastRepository>();
builder.Services.AddScoped<IWeatherForecastService, WeatherForecastService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
