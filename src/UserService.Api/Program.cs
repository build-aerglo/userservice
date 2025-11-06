using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserService.Application.Interfaces;
using UserService.Application.Services;
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

// Refresh cookie service
builder.Services.AddScoped<IRefreshTokenCookieService, RefreshTokenCookieService>();

// ---------- Domain Services ----------
builder.Services.AddScoped<IUserService, UserService.Application.Services.UserService>();

// Business service client
builder.Services.AddHttpClient<IBusinessServiceClient, BusinessServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:BusinessServiceBaseUrl"]);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Auth0 Management API
builder.Services.AddHttpClient<IAuth0ManagementService, Auth0ManagementService>();

// ---------- Cookie policy (needed for refresh cookie) ----------
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.Always;
});

// ---------- CORS ----------
var allowedOrigins = new[]
{
    "https://web-client-zeta-six.vercel.app", 
    "https://clereview.vercel.app",
    "http://localhost:5173", 
    "https://clereview-dev.vercel.app",
    "http://localhost:3000"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for refresh token cookies
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Order is important: CORS before cookies/auth
app.UseCors("FrontendPolicy");
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
