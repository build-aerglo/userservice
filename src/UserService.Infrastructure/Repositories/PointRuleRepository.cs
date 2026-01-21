using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class PointRuleRepository : IPointRuleRepository
{
    private readonly string _connectionString;

    public PointRuleRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<PointRule?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM point_rules WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PointRule>(sql, new { Id = id });
    }

    public async Task<PointRule?> GetByActionTypeAsync(string actionType)
    {
        const string sql = "SELECT * FROM point_rules WHERE action_type = @ActionType;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PointRule>(sql, new { ActionType = actionType });
    }

    public async Task<IEnumerable<PointRule>> GetAllActiveAsync()
    {
        const string sql = "SELECT * FROM point_rules WHERE is_active = true ORDER BY action_type;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<PointRule>(sql);
    }

    public async Task<IEnumerable<PointRule>> GetAllAsync()
    {
        const string sql = "SELECT * FROM point_rules ORDER BY action_type;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<PointRule>(sql);
    }

    public async Task AddAsync(PointRule rule)
    {
        const string sql = @"
            INSERT INTO point_rules (
                id, action_type, description, base_points_non_verified, 
                base_points_verified, conditions, is_active, created_at, 
                updated_at, created_by
            )
            VALUES (
                @Id, @ActionType, @Description, @BasePointsNonVerified,
                @BasePointsVerified, @Conditions::jsonb, @IsActive, @CreatedAt,
                @UpdatedAt, @CreatedBy
            );";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, rule);
    }

    public async Task UpdateAsync(PointRule rule)
    {
        const string sql = @"
            UPDATE point_rules
            SET description = @Description,
                base_points_non_verified = @BasePointsNonVerified,
                base_points_verified = @BasePointsVerified,
                conditions = @Conditions::jsonb,
                is_active = @IsActive,
                updated_at = @UpdatedAt,
                updated_by = @UpdatedBy
            WHERE id = @Id;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, rule);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM point_rules WHERE id = @Id;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}