using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class PointMultiplierRepository : IPointMultiplierRepository
{
    private readonly string _connectionString;

    public PointMultiplierRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<PointMultiplier?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM point_multipliers WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PointMultiplier>(sql, new { Id = id });
    }

    public async Task<IEnumerable<PointMultiplier>> GetAllAsync()
    {
        const string sql = "SELECT * FROM point_multipliers ORDER BY starts_at DESC;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<PointMultiplier>(sql);
    }

    public async Task<IEnumerable<PointMultiplier>> GetActiveAsync()
    {
        const string sql = "SELECT * FROM point_multipliers WHERE is_active = true ORDER BY starts_at DESC;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<PointMultiplier>(sql);
    }

    public async Task<IEnumerable<PointMultiplier>> GetCurrentlyActiveAsync()
    {
        const string sql = @"
            SELECT * FROM point_multipliers
            WHERE is_active = true AND starts_at <= @Now AND ends_at >= @Now
            ORDER BY multiplier DESC;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<PointMultiplier>(sql, new { Now = DateTime.UtcNow });
    }

    public async Task<PointMultiplier?> GetHighestActiveMultiplierAsync(string actionType)
    {
        const string sql = @"
            SELECT * FROM point_multipliers
            WHERE is_active = true AND starts_at <= @Now AND ends_at >= @Now
                AND (action_types IS NULL OR @ActionType = ANY(action_types))
            ORDER BY multiplier DESC
            LIMIT 1;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PointMultiplier>(sql, new { Now = DateTime.UtcNow, ActionType = actionType });
    }

    public async Task AddAsync(PointMultiplier multiplier)
    {
        const string sql = @"
            INSERT INTO point_multipliers (id, name, description, multiplier, action_types, starts_at, ends_at, is_active, created_at)
            VALUES (@Id, @Name, @Description, @Multiplier, @ActionTypes, @StartsAt, @EndsAt, @IsActive, @CreatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, multiplier);
    }

    public async Task UpdateAsync(PointMultiplier multiplier)
    {
        const string sql = @"
            UPDATE point_multipliers SET
                name = @Name,
                description = @Description,
                multiplier = @Multiplier,
                action_types = @ActionTypes,
                starts_at = @StartsAt,
                ends_at = @EndsAt,
                is_active = @IsActive
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, multiplier);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM point_multipliers WHERE id = @Id;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}
