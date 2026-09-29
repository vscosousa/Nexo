using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Nexo.Api.Domain.Models;
using Nexo.Api.Infrastructure;
using Nexo.Api.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Nexo.Api.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Nexo.Api.Infrastructure.Repositories;
using Nexo.Api.Controllers;
using Nexo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<NexoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("NexoDb")));

builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IPlanRepository, PlanRepository>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IAccountInvitationService, AccountInvitationService>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<PasswordHasher<Account>>();
builder.Services.AddScoped<IAccountActivationService, AccountActivationService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-XSRF-TOKEN";
    o.Cookie = new SecureCookieBuilder
    {
        Name = "nexo_xsrf",
        HttpOnly = true,
        IsEssential = true,
        SameSite = SameSiteMode.None,
    };
});

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("sign-in", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = context.RequestServices.GetRequiredService<IConfiguration>()
                .GetValue("RateLimit:SignInPermitLimit", 10),
            Window = TimeSpan.FromMinutes(1),
        }));
});

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((o, configuration) =>
    {
        o.MapInboundClaims = false;
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (!context.Request.Headers.ContainsKey("Authorization"))
                    context.Token = context.Request.Cookies[AuthController.SessionCookie];
                return Task.CompletedTask;
            },
        };
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.FromMinutes(1),
            IssuerSigningKey = configuration["Jwt:Key"] is { Length: > 0 } key
                ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
                : null,
        };
    });

var authentication = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer()
    .AddCookie(AuthController.ExternalScheme, o =>
    {
        o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        o.Cookie.SameSite = SameSiteMode.None;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
if (builder.Configuration["Authentication:Google:ClientId"] is { Length: > 0 } googleId)
    authentication.AddGoogle("google", o =>
    {
        o.ClientId = googleId;
        o.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        o.SignInScheme = AuthController.ExternalScheme;
        o.ClaimActions.MapJsonKey("email_verified", "email_verified");
    });
if (builder.Configuration["Authentication:Microsoft:ClientId"] is { Length: > 0 } microsoftId)
    authentication.AddMicrosoftAccount("microsoft", o =>
    {
        o.ClientId = microsoftId;
        o.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]!;
        o.SignInScheme = AuthController.ExternalScheme;
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    var cookieSession = context.User.Identity?.IsAuthenticated == true
        && !context.Request.Headers.ContainsKey("Authorization");
    if (cookieSession
        && !await context.RequestServices.GetRequiredService<IAntiforgery>().IsRequestValidAsync(context))
    {
        await Results.Problem("The anti-forgery token is missing or invalid.", statusCode: StatusCodes.Status400BadRequest)
            .ExecuteAsync(context);
        return;
    }
    await next();
});
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
