using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
builder.Services.AddScoped<IResourceService, ResourceService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithExposedHeaders("Retry-After")));

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
    o.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        return ValueTask.CompletedTask;
    };
    o.AddPolicy("sign-in", context => PerClientPerMinute(context, "RateLimit:SignInPermitLimit"));
    o.AddPolicy("public", context => PerClientPerMinute(context, "RateLimit:PublicPermitLimit"));

    static RateLimitPartition<string> PerClientPerMinute(HttpContext context, string limitSetting) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = context.RequestServices.GetRequiredService<IConfiguration>().GetValue(limitSetting, 10),
                Window = TimeSpan.FromMinutes(1),
            });
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
            OnTokenValidated = async context =>
            {
                var current = Guid.TryParse(context.Principal?.FindFirst("sub")?.Value, out var accountId)
                    ? await context.HttpContext.RequestServices.GetRequiredService<NexoDbContext>().Accounts
                        .Where(a => a.Id == accountId).Select(a => (int?)a.SessionVersion).SingleOrDefaultAsync()
                    : null;
                if (current is null || context.Principal!.FindFirst(TokenService.SessionVersionClaim)?.Value != current.ToString())
                    context.Fail("The session has ended.");
            },
        };
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = TokenService.Issuer(configuration),
            ValidAudience = TokenService.Audience(configuration),
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

var trustedProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownProxies.Clear();
    o.KnownIPNetworks.Clear();
    foreach (var proxy in trustedProxies)
        o.KnownProxies.Add(IPAddress.Parse(proxy));
});

var app = builder.Build();

if (trustedProxies.Length > 0)
    app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHsts();
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
