using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;


public class SupportUserProfileRepository : ISupportUserProfileRepository
{
    private readonly string _connectionString;

    public SupportUserProfileRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<IEnumerable<SupportUserProfile>> GetAllAsync()
    {
        const string sql = @"
            SELECT * 
            FROM support_user 
            ORDER BY created_at DESC;";

        await using var conn = CreateConnection();
        return await conn.QueryAsync<SupportUserProfile>(sql);
    }

    public async Task<SupportUserProfile?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM support_user WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<SupportUserProfile>(sql, new { Id = id });
    }

    public async Task<SupportUserProfile?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM support_user WHERE user_id = @UserId;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<SupportUserProfile>(sql, new { UserId = userId });
    }
    
    public async Task AddAsync(SupportUserProfile supportUserProfile)
    {
        const string sql = @"
            INSERT INTO support_user 
                (id, user_id, created_at, updated_at)
            VALUES 
                (@Id, @UserId, @CreatedAt, @UpdatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, supportUserProfile);
    }

    public async Task UpdateAsync(SupportUserProfile supportUserProfile)
    {
        const string sql = @"
            UPDATE support_user
            SET 
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, supportUserProfile);
    }
    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM support_user WHERE id = @Id;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}