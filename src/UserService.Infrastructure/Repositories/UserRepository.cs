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
    
    public async Task<Guid?> GetUserOrBusinessIdByEmailAsync(string email)
    {
        const string userSql = @"
            SELECT id, user_type 
            FROM users 
            WHERE email = @Email;
        ";
    
        using var conn = CreateConnection();
    
        // Get user record
        var user = await conn.QueryFirstOrDefaultAsync<(Guid Id, string UserType)>(
            userSql,
            new { Email = email }
        );
    
        if (user.Id == Guid.Empty)
            return null; // user not found
    
        // If NOT a business user → return user id
        if (!string.Equals(user.UserType, "business_user", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Gotten from user table");
            return user.Id;
        }
    
        // If business user → get business_id from business_reps
        const string repSql = @"
            SELECT business_id 
            FROM business_reps 
            WHERE user_id = @UserId;
        ";
    
        var businessId = await conn.ExecuteScalarAsync<Guid?>(
            repSql,
            new { UserId = user.Id }
        );

        Console.WriteLine("Gotten from business rep table");
        return businessId; // may return null if rep not found
    }

    
    public async Task<bool> EmailExistsAsync(string email)
    {
        const string sql = "SELECT COUNT(1) FROM users WHERE email = @Email;";
        using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(sql, new { Email = email });
        return count > 0;
    }
    
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
            INSERT INTO users (id, username, email, phone, user_type, address, join_date, auth0_user_id, created_at, updated_at)
            VALUES (@Id, @Username, @Email, @Phone, @UserType, @Address, @JoinDate, @Auth0UserId, @CreatedAt, @UpdatedAt)
            ON CONFLICT (email) DO NOTHING;";

        using var conn = CreateConnection();
        var rowsAffected = await conn.ExecuteAsync(sql, user);

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"User with email {user.Email} already exists in the database.");
        }
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
    
    public async Task UpdateLastLoginAsync(Guid userId, DateTime loginTime)
    {
        const string sql = @"
        UPDATE users 
        SET last_login = @LoginTime 
        WHERE id = @UserId;";
    
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { UserId = userId, LoginTime = loginTime });
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM users WHERE email = @Email;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<User?> GetByAuth0UserIdAsync(string auth0UserId)
    {
        const string sql = "SELECT * FROM users WHERE auth0_user_id = @Auth0UserId;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(sql, new { Auth0UserId = auth0UserId });
    }
}