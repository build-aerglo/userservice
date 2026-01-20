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
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<PointMultiplier?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM point_multipliers WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PointMultiplier>(sql, new { Id = id });
    }

    public async Task<IEnumerable<PointMultiplier>> GetActiveMultipliersAsync()
    {
        const string sql = @"
            SELECT * FROM point_multipliers 
            WHERE is_active = true 
            AND NOW() >= start_date 
            AND NOW() <= end_date
            ORDER BY start_date;";
        
        using var conn = CreateConnection();
        return await conn.QueryAsync<PointMultiplier>(sql);
    }

    public async Task<IEnumerable<PointMultiplier>> GetAllAsync()
    {
        const string sql = "SELECT * FROM point_multipliers ORDER BY start_date DESC;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<PointMultiplier>(sql);
    }

    public async Task AddAsync(PointMultiplier multiplier)
    {
        const string sql = @"
            INSERT INTO point_multipliers (
                id, name, description, multiplier, action_types,
                start_date, end_date, is_active, created_at, updated_at, created_by
            )
            VALUES (
                @Id, @Name, @Description, @Multiplier, @ActionTypes,
                @StartDate, @EndDate, @IsActive, @CreatedAt, @UpdatedAt, @CreatedBy
            );";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, multiplier);
    }

    public async Task UpdateAsync(PointMultiplier multiplier)
    {
        const string sql = @"
            UPDATE point_multipliers
            SET name = @Name,
                description = @Description,
                multiplier = @Multiplier,
                action_types = @ActionTypes,
                start_date = @StartDate,
                end_date = @EndDate,
                is_active = @IsActive,
                updated_at = @UpdatedAt,
                updated_by = @UpdatedBy
            WHERE id = @Id;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, multiplier);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM point_multipliers WHERE id = @Id;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}