using Npgsql;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UserService.Infrastructure.Database;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync();
}

public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly ILogger<NpgsqlConnectionFactory> _logger;

    public NpgsqlConnectionFactory(IConfiguration configuration, ILogger<NpgsqlConnectionFactory> logger)
    {
        _connectionString = configuration.GetConnectionString("PostgresConnection")!;
        _logger = logger;

        // Set Dapper mappings once
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public async Task<IDbConnection> CreateConnectionAsync()
    {
        const int maxRetries = 3;
        const int retryDelayMs = 1000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            var connection = new NpgsqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                _logger.LogDebug("Database connected on attempt {Attempt}", attempt);
                return connection;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                await connection.DisposeAsync();
                _logger.LogWarning(ex, "Database connection attempt {Attempt} failed. Retrying in {Delay}ms", attempt, retryDelayMs);
                await Task.Delay(retryDelayMs);
            }
            catch (Exception ex)
            {
                await connection.DisposeAsync();
                _logger.LogError(ex, "Database connection failed after {MaxRetries} attempts", maxRetries);
                throw;
            }
        }

        // Unreachable, but satisfies the compiler
        throw new InvalidOperationException("Failed to connect to database after multiple attempts.");
    }
}
