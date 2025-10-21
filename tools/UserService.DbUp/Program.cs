using System.Reflection;
using DbUp;
using Microsoft.Extensions.Configuration;

// Build configuration from appsettings.json or environment
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = config.GetConnectionString("PostgresConnection")
                       ?? "Host=localhost;Port=5432;Database=ReviewApp;Username=postgres;Password=admin;Include Error Detail=true;";

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("🚀 Starting database migration...");

// Ensure the target database exists (optional)
EnsureDatabase.For.PostgresqlDatabase(connectionString);

// Configure the upgrader
var upgrader =
    DeployChanges.To
        .PostgresqlDatabase(connectionString)
        .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
        .LogToConsole()
        .Build();

// Run migrations
var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("❌ Migration failed!");
    Console.WriteLine(result.Error);
    Console.ResetColor();
    return -1;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("✅ Migration successful!");
Console.ResetColor();
return 0;