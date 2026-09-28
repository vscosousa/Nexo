using Microsoft.AspNetCore.Identity;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure.Email;
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

builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IAccountInvitationService, AccountInvitationService>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<PasswordHasher<Account>>();
builder.Services.AddScoped<IAccountActivationService, AccountActivationService>();

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
