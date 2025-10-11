using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        const string sql = "SELECT * FROM users ORDER BY created_at DESC;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<User>(sql);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM users WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task AddAsync(User user)
    {
        const string sql = @"
            INSERT INTO users (id, username, email, phone, user_type, address, join_date, created_at, updated_at)
            VALUES (@Id, @Username, @Email, @Phone, @UserType, @Address, @JoinDate, @CreatedAt, @UpdatedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, user);
    }

    public async Task UpdateAsync(User user)
    {
        const string sql = @"
            UPDATE users
            SET email = @Email,
                phone = @Phone,
                address = @Address,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, user);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM users WHERE id = @Id;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}