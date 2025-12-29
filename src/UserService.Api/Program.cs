using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Application.Services.Auth0;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Clients;
using UserService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

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
//  BUSINESS SERVICE CLIENT — ALLOW HTTP (FIX FOR SSL MISMATCH ERROR)
// ==================================================================
builder.Services.AddHttpClient<IBusinessServiceClient, BusinessServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:BusinessServiceBaseUrl"]);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    // 👇 THIS FIXES YOUR ERROR: Allow HTTP, do NOT enforce SSL
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
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

// ---------- CORS ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)  // allow temporary until production
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
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
                Console.WriteLine("❌ Token auth failed: " + ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                Console.WriteLine("✅ Token validated");
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

// Swagger always enabled
app.UseSwagger();
app.UseSwaggerUI();

// Correct order
app.UseCors("FrontendPolicy");
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.Run();
