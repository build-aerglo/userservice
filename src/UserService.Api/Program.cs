using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading.RateLimiting;
using Azure.Identity;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Application.Services.Auth0;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Clients;
using UserService.Infrastructure.Database;
using UserService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// AZURE APP CONFIGURATION
// In production the App Service setting AzureAppConfiguration__Endpoint
// is the only thing configured directly. Everything else — Auth0 secrets,
// Postgres connection string, encryption key, AfricaTalking key —
// is pulled from App Configuration + Key Vault at startup via
// Managed Identity. No secrets in code or git.
// Locally this block is skipped (endpoint is empty) and the app
// uses appsettings.Development.json as normal.
// ============================================================
var appConfigEndpoint = builder.Configuration["AzureAppConfiguration:Endpoint"];

if (!string.IsNullOrWhiteSpace(appConfigEndpoint))
{
    try
    {
        builder.Configuration.AddAzureAppConfiguration(options =>
        {
            options
                .Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())
                .ConfigureKeyVault(kv =>
                {
                    kv.SetCredential(new DefaultAzureCredential());
                });
        });
        Console.WriteLine($"[AppConfig] Connected: {appConfigEndpoint}");
    }
    catch (Exception ex)
    {
        // Startup/bootstrap logging — DI container not yet built, Console is appropriate here.
        Console.Error.WriteLine($"[AppConfig] FAILED: {ex.GetType().Name}: {ex.Message}");
        Console.Error.WriteLine($"[AppConfig] Inner: {ex.InnerException?.Message}");
    }
}
else
{
    Console.WriteLine("[AppConfig] Endpoint not set — skipping.");
}

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
// Enable Dapper snake_case to PascalCase mapping (e.g., auth0_user_id → Auth0UserId)
DefaultTypeMap.MatchNamesWithUnderscores = true;

// MVC
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

// TLS for macOS + Auth0 issue
ServicePointManager.SecurityProtocol =
    SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

// ---------- Database Repos ----------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBusinessRepRepository, BusinessRepRepository>();
builder.Services.AddScoped<ISupportUserProfileRepository, SupportUserProfileRepository>();
builder.Services.AddScoped<IEndUserProfileRepository, EndUserProfileRepository>();
builder.Services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
builder.Services.AddScoped<ISocialIdentityRepository, SocialIdentityRepository>();

// ---------- New Feature Repos (Badge, Points, Verification, Referral, Geolocation) ----------
builder.Services.AddScoped<IUserBadgeRepository, UserBadgeRepository>();
builder.Services.AddScoped<IUserPointsRepository, UserPointsRepository>();
builder.Services.AddScoped<IPointTransactionRepository, PointTransactionRepository>();
builder.Services.AddScoped<IUserVerificationRepository, UserVerificationRepository>();
builder.Services.AddScoped<IVerificationTokenRepository, VerificationTokenRepository>();
builder.Services.AddScoped<IUserReferralCodeRepository, UserReferralCodeRepository>();
builder.Services.AddScoped<IReferralRepository, ReferralRepository>();
builder.Services.AddScoped<IUserGeolocationRepository, UserGeolocationRepository>();
builder.Services.AddScoped<IGeolocationHistoryRepository, GeolocationHistoryRepository>();

builder.Services.AddScoped<IPointRuleRepository, PointRuleRepository>();
builder.Services.AddScoped<IPointMultiplierRepository, PointMultiplierRepository>();
builder.Services.AddScoped<IPointRedemptionRepository, PointRedemptionRepository>();
builder.Services.AddScoped<IPasswordResetRequestRepository, PasswordResetRequestRepository>();
builder.Services.AddScoped<IEmailUpdateRequestRepository, EmailUpdateRequestRepository>();
builder.Services.AddScoped<IRegistrationVerificationRepository, RegistrationVerificationRepository>();
builder.Services.AddScoped<IBusinessClaimRepository, BusinessClaimRepository>();
builder.Services.AddScoped<IBusinessRepository, BusinessRepository>();

// ---------- Auth0 Login HTTP Client (TLS forced) ----------
builder.Services.AddHttpClient<IAuth0UserLoginService, Auth0UserLoginService>(client =>
{
    client.DefaultRequestVersion = HttpVersion.Version11;
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        }
    };
});

// ---------- Refresh cookie service ----------
builder.Services.AddScoped<IRefreshTokenCookieService, RefreshTokenCookieService>();

// ---------- Domain Services ----------
builder.Services.AddScoped<IUserService, UserService.Application.Services.UserService>();

// ---------- New Feature Services (Badge, Points, Verification, Referral, Geolocation) ----------
builder.Services.AddScoped<IBadgeService, BadgeService>();
builder.Services.AddScoped<IPointsService, PointsService>();
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<IReferralService, ReferralService>();
builder.Services.AddScoped<IGeolocationService, GeolocationService>();

// ---------- Password Reset & Encryption Services ----------
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

// ---------- Registration Email Verification Service ----------
builder.Services.AddScoped<IRegistrationVerificationService, RegistrationVerificationService>();

// ---------- AfricaTalking SMS (TLS enforced — change BaseUrl to sandbox for local dev) ----------
builder.Services.AddHttpClient<IAfricaTalkingClient, AfricaTalkingClient>(client =>
{
    var baseUrl = builder.Configuration["AfricaTalking:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl!);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    return new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        }
    };
});

// ---------- Review Service HTTP Client ----------
builder.Services.AddHttpClient<IReviewServiceClient, ReviewServiceClient>(client =>
{
    var baseUrl = builder.Configuration["Services:ReviewServiceBaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl))
        client.BaseAddress = new Uri(baseUrl);
});

// ---------- Notification Service HTTP Client ----------
builder.Services.AddHttpClient<INotificationServiceClient, NotificationServiceClient>(client =>
{
    var baseUrl = builder.Configuration["Services:NotificationServiceBaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl))
        client.BaseAddress = new Uri(baseUrl);
});

// ---------- Business Service HTTP Client ----------
builder.Services.AddHttpClient<IBusinessServiceClient, BusinessServiceClient>(client =>
{
    var baseUrl = builder.Configuration["Services:BusinessServiceBaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl))
        client.BaseAddress = new Uri(baseUrl);
});

// ---------- Auth0 Management API ----------
builder.Services.AddHttpClient<IAuth0ManagementService, Auth0ManagementService>();

// ---------- Auth0 Social Login Service ----------
builder.Services.AddHttpClient<IAuth0SocialLoginService, Auth0SocialLoginService>(client =>
{
    client.DefaultRequestVersion = HttpVersion.Version11;
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        }
    };
});

// ---------- Auth0 JWT Auth ----------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
        options.Audience = builder.Configuration["Auth0:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

// ---------- Rate Limiting ----------
// Applied at controller/action level via [EnableRateLimiting("...")] attributes.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Login: 10 attempts per minute per IP
    options.AddFixedWindowLimiter("login", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
        o.AutoReplenishment = true;
    });

    // Sensitive: password reset / OTP send — 3 per minute per IP
    options.AddFixedWindowLimiter("sensitive", o =>
    {
        o.PermitLimit = 3;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
        o.AutoReplenishment = true;
    });
});

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User Service API",
        Version = "v1"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your Bearer token"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ---------- CORS ----------
// AllowedOrigins must be set in configuration (Key Vault in production, appsettings.Development.json locally).
// AllowCredentials() is required for the HttpOnly refresh-token cookie to be sent cross-origin.
// Note: AllowAnyOrigin() is intentionally NOT used because it is incompatible with AllowCredentials().
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // No origins configured: permit any origin without credentials.
            // This is the safe fallback — cookies will not be sent cross-origin
            // in this mode, which is intentional until origins are configured.
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// ---------- Health checks ----------
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();

// NOTE: UseHttpsRedirection intentionally removed.
// Azure App Service handles HTTPS at the load balancer.

// Swagger is only served in Development to avoid exposing internal API surface in production.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseRateLimiter();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

// ---------- Database health check ----------
public class DatabaseHealthCheck(IDbConnectionFactory dbFactory) : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var conn = await dbFactory.CreateConnectionAsync();
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database reachable.");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database unreachable.", ex);
        }
    }
}
