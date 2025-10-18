using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using UserService.Application.Services;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------
// 🔧 SERVICE CONFIGURATION
// -------------------------------

// 1️⃣ Add Controllers (instead of minimal APIs)
builder.Services.AddControllers();

// 2️⃣ Register Dapper Repository + Application Service
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEndUsersRepository, EndUsersRepository>();
builder.Services.AddScoped<IUserService, UserService.Application.Services.UserService>();


// 3️⃣ Configure Auth0 Authentication
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

// 4️⃣ Authorization
builder.Services.AddAuthorization();

// 5️⃣ Add Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User Service API",
        Version = "v1",
        Description = "User service Microservice implemented in DDD"
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
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

app.UseHttpsRedirection();

// 1️⃣ Swagger setup
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 2️⃣ Authentication + Authorization
app.UseAuthentication();    
app.UseAuthorization();

// 3️⃣ Map Controllers (routes defined via [Route] + [HttpGet]/[HttpPost])
app.MapControllers();

// -------------------------------
app.Run();
