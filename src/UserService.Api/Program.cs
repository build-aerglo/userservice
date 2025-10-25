using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Clients;
using UserService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------
// 🔧 SERVICE CONFIGURATION
// -------------------------------

// 1️⃣ Add Controllers (instead of minimal APIs)
builder.Services.AddControllers();

// 2️⃣ Register Dapper Repositories + Application Services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBusinessRepRepository, BusinessRepRepository>();
builder.Services.AddScoped<IUserService, UserService.Application.Services.UserService>();
builder.Services.AddScoped<ISupportUserProfileRepository, SupportUserProfileRepository>();
builder.Services.AddScoped<IEndUserProfileRepository, EndUserProfileRepository>();

// 3️⃣ Register HttpClient for Business Service
builder.Services.AddHttpClient<IBusinessServiceClient, BusinessServiceClient>(client =>
{
    var baseUrl = builder.Configuration["Services:BusinessServiceBaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("Missing configuration: Services:BusinessServiceBaseUrl");

    client.BaseAddress = new Uri(baseUrl);

    // Optional: Set defaults like timeout or headers
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// 4️⃣ Configure Auth0 Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var domain = builder.Configuration["Auth0:Domain"];
        var audience = builder.Configuration["Auth0:Audience"];

        options.Authority = $"https://{domain}/";
        options.Audience = audience;

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("❌ Authentication failed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("✅ Token validated successfully");
                return Task.CompletedTask;
            }
        };
    });

// 5️⃣ Authorization
builder.Services.AddAuthorization();

// 6️⃣ Swagger with JWT Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User Service API",
        Version = "v1",
        Description = "User Service Microservice implemented with DDD"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your Auth0 access token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIs..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// -------------------------------
// 🚀 BUILD APP
// -------------------------------
var app = builder.Build();

// ✅ Dapper naming convention fix
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// 🔒 HTTPS
app.UseHttpsRedirection();

// 1️⃣ Swagger setup
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API v1");
        options.RoutePrefix = string.Empty; 
    });
}

// 2️⃣ Authentication + Authorization
app.UseAuthentication();    
app.UseAuthorization();

// 3️⃣ Map Controllers
app.MapControllers();

// -------------------------------
app.Run();
