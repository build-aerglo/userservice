using Npgsql;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace UserService.Infrastructure.Database;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync();
}

public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgresConnection")!;
        
        // Set Dapper mappings once
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public async Task<IDbConnection> CreateConnectionAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        
        // Add retry logic
        var maxRetries = 3;
        var retryDelay = 1000;
        
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await connection.OpenAsync();
                Console.WriteLine($"✅ Database connected successfully (attempt {i + 1})");
                return connection;
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                Console.WriteLine($"⚠️ Connection attempt {i + 1} failed: {ex.Message}. Retrying in {retryDelay}ms...");
                await Task.Delay(retryDelay);
                connection = new NpgsqlConnection(_connectionString);
            }
        }
        
        throw new Exception("Failed to connect to database after multiple attempts");
    }
}