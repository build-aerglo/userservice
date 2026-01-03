using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class UserDailyPointsRepository : IUserDailyPointsRepository
{
    private readonly string _connectionString;

    public UserDailyPointsRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserDailyPoints?> GetByUserActionDateAsync(Guid userId, string actionType, DateTime date)
    {
        const string sql = @"
            SELECT * FROM user_daily_points
            WHERE user_id = @UserId AND action_type = @ActionType AND occurrence_date = @Date;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserDailyPoints>(sql, new { UserId = userId, ActionType = actionType, Date = date.Date });
    }

    public async Task<IEnumerable<UserDailyPoints>> GetByUserIdAsync(Guid userId, DateTime date)
    {
        const string sql = @"
            SELECT * FROM user_daily_points
            WHERE user_id = @UserId AND occurrence_date = @Date;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<UserDailyPoints>(sql, new { UserId = userId, Date = date.Date });
    }

    public async Task AddAsync(UserDailyPoints dailyPoints)
    {
        const string sql = @"
            INSERT INTO user_daily_points (id, user_id, action_type, occurrence_date, occurrence_count, last_occurrence_at)
            VALUES (@Id, @UserId, @ActionType, @OccurrenceDate, @OccurrenceCount, @LastOccurrenceAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, dailyPoints);
    }

    public async Task UpdateAsync(UserDailyPoints dailyPoints)
    {
        const string sql = @"
            UPDATE user_daily_points SET
                occurrence_count = @OccurrenceCount,
                last_occurrence_at = @LastOccurrenceAt
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, dailyPoints);
    }

    public async Task UpsertAsync(UserDailyPoints dailyPoints)
    {
        const string sql = @"
            INSERT INTO user_daily_points (id, user_id, action_type, occurrence_date, occurrence_count, last_occurrence_at)
            VALUES (@Id, @UserId, @ActionType, @OccurrenceDate, @OccurrenceCount, @LastOccurrenceAt)
            ON CONFLICT (user_id, action_type, occurrence_date) DO UPDATE SET
                occurrence_count = user_daily_points.occurrence_count + 1,
                last_occurrence_at = EXCLUDED.last_occurrence_at;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, dailyPoints);
    }

    public async Task CleanupOldRecordsAsync(int daysToKeep = 7)
    {
        const string sql = "DELETE FROM user_daily_points WHERE occurrence_date < @CutoffDate;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { CutoffDate = DateTime.UtcNow.Date.AddDays(-daysToKeep) });
    }
}
