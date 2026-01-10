using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Application.Services.Auth0;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Clients;
using UserService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// DEBUG: Print all AZURE environment variables
// ============================================================================
Console.WriteLine("[ENV DEBUG] Checking all AZURE environment variables:");
foreach (System.Collections.DictionaryEntry env in Environment.GetEnvironmentVariables())
{
    if (env.Key.ToString()?.Contains("AZURE", StringComparison.OrdinalIgnoreCase) == true)
    {
        var value = env.Value?.ToString();
        Console.WriteLine($"  {env.Key} = {(string.IsNullOrEmpty(value) ? "EMPTY" : $"SET (length: {value.Length})")}");
    }
}
Console.WriteLine($"[ENV DEBUG] ASPNETCORE_ENVIRONMENT = {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");

// ============================================================================
// AZURE CONFIGURATION - Key Vault & App Configuration Integration
// ============================================================================
var azureAppConfigConnectionString = Environment.GetEnvironmentVariable("AZURE_APP_CONFIGURATION_CONNECTION_STRING");
var keyVaultUri = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_URI");

Console.WriteLine($"[CONFIG DEBUG] Azure App Config Connection String: {(string.IsNullOrEmpty(azureAppConfigConnectionString) ? "NOT SET" : "SET (length: " + azureAppConfigConnectionString.Length + ")")}");
Console.WriteLine($"[CONFIG DEBUG] Key Vault URI: {(string.IsNullOrEmpty(keyVaultUri) ? "NOT SET" : keyVaultUri)}");

// Add Azure App Configuration if connection string is provided
if (!string.IsNullOrEmpty(azureAppConfigConnectionString))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(azureAppConfigConnectionString)
            .Select(KeyFilter.Any, LabelFilter.Null)
            .Select(KeyFilter.Any, builder.Environment.EnvironmentName)
            .ConfigureRefresh(refresh =>
            {
                refresh.Register("Settings:Sentinel", refreshAll: true)
                    .SetCacheExpiration(TimeSpan.FromMinutes(5));
            });

        // Connect to Key Vault for secrets referenced in App Configuration
        if (!string.IsNullOrEmpty(keyVaultUri))
        {
            options.ConfigureKeyVault(kv =>
            {
                kv.SetCredential(new DefaultAzureCredential());
            });
        }
    });

    // Add Azure App Configuration middleware for dynamic refresh
    builder.Services.AddAzureAppConfiguration();
}

// Add Azure Key Vault directly if URI is provided (for secrets not in App Config)
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

// ============================================================================
// ENVIRONMENT VARIABLE OVERRIDES
// ============================================================================
// All configuration can be overridden via environment variables using __ as separator
// Examples:
//   ConnectionStrings__PostgresConnection -> ConnectionStrings:PostgresConnection
//   Auth0__ClientSecret -> Auth0:ClientSecret
//   Services__BusinessServiceBaseUrl -> Services:BusinessServiceBaseUrl

builder.Configuration.AddEnvironmentVariables();

// Debug: Print loaded Auth0 configuration (mask secrets)
Console.WriteLine($"[CONFIG DEBUG] Auth0:Domain = {builder.Configuration["Auth0:Domain"]}");
Console.WriteLine($"[CONFIG DEBUG] Auth0:Audience = {builder.Configuration["Auth0:Audience"]}");
Console.WriteLine($"[CONFIG DEBUG] Auth0:ClientId = {builder.Configuration["Auth0:ClientId"]}");
Console.WriteLine($"[CONFIG DEBUG] Auth0:ClientSecret = {(string.IsNullOrEmpty(builder.Configuration["Auth0:ClientSecret"]) ? "NOT SET" : "SET (length: " + builder.Configuration["Auth0:ClientSecret"]?.Length + ")")}");
Console.WriteLine($"[CONFIG DEBUG] Auth0:DbConnection = {builder.Configuration["Auth0:DbConnection"]}");
Console.WriteLine($"[CONFIG DEBUG] ConnectionStrings:PostgresConnection = {(string.IsNullOrEmpty(builder.Configuration.GetConnectionString("PostgresConnection")) ? "NOT SET" : "SET")}");

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

// ==================================================================
// BUSINESS SERVICE CLIENT - Secure SSL Configuration
// ==================================================================
var businessServiceBaseUrl = builder.Configuration["Services:BusinessServiceBaseUrl"];
builder.Services.AddHttpClient<IBusinessServiceClient, BusinessServiceClient>(client =>
{
    if (!string.IsNullOrEmpty(businessServiceBaseUrl))
    {
        client.BaseAddress = new Uri(businessServiceBaseUrl);
    }
    client.DefaultRequestHeaders.Add("Accept", "application/json");
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

// ---------- Cookie policy (needed for refresh cookie) ----------
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.Always;
});

// ---------- CORS - Secure Configuration ----------
// Read allowed origins from configuration (can be set via environment variables or Azure App Config)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "https://aerglotechnology.com", "https://www.aerglotechnology.com" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    // Development policy - only for non-production environments
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("DevelopmentPolicy", policy =>
        {
            policy
                .SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    }
});

// ---------- Auth0 JWT Auth ----------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var domain = builder.Configuration["Auth0:Domain"];
        var audience = builder.Configuration["Auth0:Audience"];

        options.Authority = $"https://{domain}/";
        options.Audience = audience;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = "https://user-service.aerglotechnology.com/roles"
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("Token auth failed: " + ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                Console.WriteLine("Token validated");
                return Task.CompletedTask;
            }
        };
    });

// ---------- Authorization Policies ----------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BusinessOnly", p => p.RequireRole("business_user"));
    options.AddPolicy("SupportOnly", p => p.RequireRole("support_user"));
    options.AddPolicy("EndUserOnly", p => p.RequireRole("end_user"));
    options.AddPolicy("BizOrSupport", p => p.RequireRole("business_user", "support_user"));
});

// ---------- Swagger + JWT ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "User Service API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
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

// Build
var app = builder.Build();

// Use Azure App Configuration middleware for dynamic refresh (if configured)
if (!string.IsNullOrEmpty(azureAppConfigConnectionString))
{
    app.UseAzureAppConfiguration();
}

// Swagger - only in Development or if explicitly enabled
var enableSwagger = builder.Configuration.GetValue<bool>("EnableSwagger")
    || string.Equals(builder.Configuration["EnableSwagger"], "true", StringComparison.OrdinalIgnoreCase);
if (app.Environment.IsDevelopment() || enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Correct order
var corsPolicy = app.Environment.IsDevelopment() ? "DevelopmentPolicy" : "FrontendPolicy";
app.UseCors(corsPolicy);
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.Run();
