using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class EndUserProfileRepository : IEndUserProfileRepository
{
    private readonly string _connectionString;

    public EndUserProfileRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true; // Ensures snake_case ↔ PascalCase mapping
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task AddAsync(EndUserProfile profile)
    {
        const string sql = @"
            INSERT INTO end_user (id, user_id, social_media, created_at, updated_at)
            VALUES (@Id, @UserId, @SocialMedia, @CreatedAt, @UpdatedAt)
            ON CONFLICT (user_id) DO NOTHING;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, profile);
    }

    public async Task<EndUserProfile?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM end_user WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<EndUserProfile>(sql, new { Id = id });
    }

    public async Task<EndUserProfile?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM end_user WHERE user_id = @UserId;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<EndUserProfile>(sql, new { UserId = userId });
    }

    public async Task UpdateAsync(EndUserProfile profile)
    {
        const string sql = @"
            UPDATE end_user
            SET social_media = @SocialMedia,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, profile);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM end_user WHERE id = @Id;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}