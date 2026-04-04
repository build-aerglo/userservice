using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using Azure.Identity;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        Console.WriteLine($"[AppConfig] FAILED: {ex.GetType().Name}: {ex.Message}");
        Console.WriteLine($"[AppConfig] Inner: {ex.InnerException?.Message}");
    }
}
else
{
    Console.WriteLine("[AppConfig] Endpoint not set — skipping.");
}

builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
// Enable Dapper snake_case to PascalCase mapping (e.g., auth0_user_id → Auth0UserId)
DefaultTypeMap.MatchNamesWithUnderscores = true;

// MVC
builder.Services.AddControllers();

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
builder.Services.AddScoped<IGeolocationService,GeolocationService>();

// ---------- Password Reset & Encryption Services ----------
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

// ---------- Registration Email Verification Service ----------
builder.Services.AddScoped<IRegistrationVerificationService, RegistrationVerificationService>();

// ---------- AfricaTalking SMS ----------
builder.Services.AddHttpClient<IAfricaTalkingClient, AfricaTalkingClient>(client =>
{
    var baseUrl = builder.Configuration["AfricaTalking:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl!);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    // Allow HTTP, do NOT enforce SSL
    return new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = SslProtocols.None
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// NOTE: UseHttpsRedirection intentionally removed.
// Azure App Service handles HTTPS at the load balancer.

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API v1");
    options.RoutePrefix = "";
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();