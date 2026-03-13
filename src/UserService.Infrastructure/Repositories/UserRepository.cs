using Dapper;
using Npgsql;
using System.Data;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Database;

namespace UserService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDbConnection? _testConnection;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public UserRepository(IDbConnection testConnection)
    {
        _testConnection = testConnection;
    }

    private bool IsTestMode => _testConnection != null;

    private async Task<T> QueryAsync<T>(Func<IDbConnection, Task<T>> query)
    {
        if (IsTestMode)
        {
            if (_testConnection!.State != ConnectionState.Open)
                await ((System.Data.Common.DbConnection)_testConnection).OpenAsync();
            return await query(_testConnection);
        }

        using var conn = await _connectionFactory.CreateConnectionAsync();
        return await query(conn);
    }

    private async Task ExecuteAsync(Func<IDbConnection, Task> command)
    {
        if (IsTestMode)
        {
            if (_testConnection!.State != ConnectionState.Open)
                await ((System.Data.Common.DbConnection)_testConnection).OpenAsync();
            await command(_testConnection);
            return;
        }

        using var conn = await _connectionFactory.CreateConnectionAsync();
        await command(conn);
    }

    public async Task<Guid?> GetUserOrBusinessIdByEmailAsync(string email)
    {
        return await QueryAsync(async conn =>
        {
            const string userSql = "SELECT id, user_type FROM users WHERE LOWER(email) = LOWER(@Email);";
            var user = await conn.QueryFirstOrDefaultAsync<(Guid Id, string UserType)>(
                userSql, new { Email = email });

            if (user.Id == Guid.Empty) return (Guid?)null;

            if (!string.Equals(user.UserType, "business_user", StringComparison.OrdinalIgnoreCase))
                return user.Id;

            const string repSql = "SELECT business_id FROM business_reps WHERE user_id = @UserId;";
            return await conn.ExecuteScalarAsync<Guid?>(repSql, new { UserId = user.Id });
        });
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        const string sql = "SELECT COUNT(1) FROM users WHERE email = @Email;";
        return await QueryAsync(async conn =>
            await conn.ExecuteScalarAsync<int>(sql, new { Email = email }) > 0);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        const string sql = "SELECT * FROM users ORDER BY created_at DESC;";
        return await QueryAsync(conn => conn.QueryAsync<User>(sql));
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM users WHERE id = @Id;";
        return await QueryAsync(conn =>
            conn.QueryFirstOrDefaultAsync<User>(sql, new { Id = id }));
    }

    public async Task AddAsync(User user)
    {
        const string sql = @"
            INSERT INTO users (id, username, email, phone, user_type, address, join_date, created_at, updated_at, login_type, auth0_user_id)
            VALUES (@Id, @Username, @Email, @Phone, @UserType, @Address, @JoinDate, @CreatedAt, @UpdatedAt, @LoginType, @Auth0UserId);";

        await ExecuteAsync(conn => conn.ExecuteAsync(sql, user));
    }

    public async Task UpdateAsync(User user)
    {
        const string sql = @"
            UPDATE users
            SET email      = @Email,
                phone      = @Phone,
                address    = @Address,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        await ExecuteAsync(conn => conn.ExecuteAsync(sql, user));
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM users WHERE id = @Id;";
        await ExecuteAsync(conn => conn.ExecuteAsync(sql, new { Id = id }));
    }

    public async Task UpdateLastLoginAsync(Guid userId, DateTime loginTime)
    {
        const string sql = "UPDATE users SET last_login = @LoginTime WHERE id = @UserId;";
        await ExecuteAsync(conn =>
            conn.ExecuteAsync(sql, new { UserId = userId, LoginTime = loginTime }));
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM users WHERE LOWER(email) = LOWER(@Email);";
        return await QueryAsync(conn =>
            conn.QueryFirstOrDefaultAsync<User>(sql, new { Email = email }));
    }

    public async Task<User?> GetByPhoneAsync(string phone)
    {
        const string sql = "SELECT * FROM users WHERE phone = @Phone;";
        return await QueryAsync(conn =>
            conn.QueryFirstOrDefaultAsync<User>(sql, new { Phone = phone }));
    }

    public async Task<bool> PhoneExistsAsync(string phone)
    {
        const string sql = "SELECT COUNT(1) FROM users WHERE phone = @Phone;";
        return await QueryAsync(async conn =>
            await conn.ExecuteScalarAsync<int>(sql, new { Phone = phone }) > 0);
    }

    public async Task<User?> GetByEmailOrPhoneAsync(string identifier)
    {
        const string sql = "SELECT * FROM users WHERE LOWER(email) = LOWER(@Identifier) OR phone = @Identifier;";
        return await QueryAsync(conn =>
            conn.QueryFirstOrDefaultAsync<User>(sql, new { Identifier = identifier }));
    }

    public async Task UpdateEmailAsync(Guid userId, string newEmail)
    {
        const string sql = @"
            UPDATE users
            SET email      = @NewEmail,
                updated_at = @UpdatedAt
            WHERE id = @UserId;";

        await ExecuteAsync(conn =>
            conn.ExecuteAsync(sql, new { UserId = userId, NewEmail = newEmail, UpdatedAt = DateTime.UtcNow }));
    }

    public async Task SetUserIdAsync(Guid userId, Guid businessId)
    {
        const string sql = "UPDATE business SET user_id = @UserId WHERE id = @BusinessId;";
        await ExecuteAsync(conn =>
            conn.ExecuteAsync(sql, new { UserId = userId, BusinessId = businessId }));
    }

    public async Task UpdateEmailVerifiedAsync(Guid userId)
    {
        const string sql = @"
            UPDATE users
            SET is_email_verified = TRUE,
                updated_at = @UpdatedAt
            WHERE id = @UserId;";

        await ExecuteAsync(conn =>
            conn.ExecuteAsync(sql, new { UserId = userId, UpdatedAt = DateTime.UtcNow }));
    }
}