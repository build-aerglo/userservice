using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class BadgeDefinitionRepository : IBadgeDefinitionRepository
{
    private readonly string _connectionString;

    public BadgeDefinitionRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<BadgeDefinition?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM badge_definitions WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<BadgeDefinition>(sql, new { Id = id });
    }

    public async Task<BadgeDefinition?> GetByNameAsync(string name)
    {
        const string sql = "SELECT * FROM badge_definitions WHERE name = @Name;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<BadgeDefinition>(sql, new { Name = name });
    }

    public async Task<IEnumerable<BadgeDefinition>> GetAllAsync()
    {
        const string sql = "SELECT * FROM badge_definitions ORDER BY tier, points_required;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<BadgeDefinition>(sql);
    }

    public async Task<IEnumerable<BadgeDefinition>> GetActiveAsync()
    {
        const string sql = "SELECT * FROM badge_definitions WHERE is_active = true ORDER BY tier, points_required;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<BadgeDefinition>(sql);
    }

    public async Task<IEnumerable<BadgeDefinition>> GetByCategoryAsync(string category)
    {
        const string sql = "SELECT * FROM badge_definitions WHERE category = @Category AND is_active = true ORDER BY tier;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<BadgeDefinition>(sql, new { Category = category });
    }

    public async Task<IEnumerable<BadgeDefinition>> GetByTierAsync(int tier)
    {
        const string sql = "SELECT * FROM badge_definitions WHERE tier = @Tier AND is_active = true;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<BadgeDefinition>(sql, new { Tier = tier });
    }

    public async Task<IEnumerable<BadgeDefinition>> GetByPointsRequiredAsync(int maxPoints)
    {
        const string sql = "SELECT * FROM badge_definitions WHERE points_required <= @MaxPoints AND is_active = true ORDER BY points_required;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<BadgeDefinition>(sql, new { MaxPoints = maxPoints });
    }

    public async Task AddAsync(BadgeDefinition badge)
    {
        const string sql = @"
            INSERT INTO badge_definitions (id, name, display_name, description, icon_url, tier, points_required, category, is_active, created_at, updated_at)
            VALUES (@Id, @Name, @DisplayName, @Description, @IconUrl, @Tier, @PointsRequired, @Category, @IsActive, @CreatedAt, @UpdatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, badge);
    }

    public async Task UpdateAsync(BadgeDefinition badge)
    {
        const string sql = @"
            UPDATE badge_definitions SET
                display_name = @DisplayName,
                description = @Description,
                icon_url = @IconUrl,
                tier = @Tier,
                points_required = @PointsRequired,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, badge);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM badge_definitions WHERE id = @Id;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}
