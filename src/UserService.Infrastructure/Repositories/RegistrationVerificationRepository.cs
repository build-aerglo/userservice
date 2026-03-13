using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class RegistrationVerificationRepository : IRegistrationVerificationRepository
{
    private readonly string _connectionString;

    public RegistrationVerificationRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task AddAsync(RegistrationVerification verification)
    {
        const string sql = @"
            INSERT INTO registration_verification (id, email, username, token, expiry, user_type, created_at)
            VALUES (@Id, @Email, @Username, @Token, @Expiry, @UserType, @CreatedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, verification);
    }

    public async Task<RegistrationVerification?> GetByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM registration_verification WHERE LOWER(email) = LOWER(@Email) ORDER BY created_at DESC LIMIT 1;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<RegistrationVerification>(sql, new { Email = email });
    }

    public async Task DeleteByEmailAsync(string email)
    {
        const string sql = "DELETE FROM registration_verification WHERE LOWER(email) = LOWER(@Email);";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Email = email });
    }
}
