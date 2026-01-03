using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class UserGeofenceEventRepository : IUserGeofenceEventRepository
{
    private readonly string _connectionString;

    public UserGeofenceEventRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserGeofenceEvent?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM user_geofence_events WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserGeofenceEvent>(sql, new { Id = id });
    }

    public async Task<IEnumerable<UserGeofenceEvent>> GetByUserIdAsync(Guid userId, int limit = 100)
    {
        const string sql = @"
            SELECT * FROM user_geofence_events
            WHERE user_id = @UserId
            ORDER BY triggered_at DESC
            LIMIT @Limit;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<UserGeofenceEvent>(sql, new { UserId = userId, Limit = limit });
    }

    public async Task<IEnumerable<UserGeofenceEvent>> GetByGeofenceIdAsync(Guid geofenceId, int limit = 100)
    {
        const string sql = @"
            SELECT * FROM user_geofence_events
            WHERE geofence_id = @GeofenceId
            ORDER BY triggered_at DESC
            LIMIT @Limit;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<UserGeofenceEvent>(sql, new { GeofenceId = geofenceId, Limit = limit });
    }

    public async Task<IEnumerable<UserGeofenceEvent>> GetByUserIdAndDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        const string sql = @"
            SELECT * FROM user_geofence_events
            WHERE user_id = @UserId AND triggered_at >= @StartDate AND triggered_at <= @EndDate
            ORDER BY triggered_at DESC;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<UserGeofenceEvent>(sql, new { UserId = userId, StartDate = startDate, EndDate = endDate });
    }

    public async Task<UserGeofenceEvent?> GetLatestByUserAndGeofenceAsync(Guid userId, Guid geofenceId)
    {
        const string sql = @"
            SELECT * FROM user_geofence_events
            WHERE user_id = @UserId AND geofence_id = @GeofenceId
            ORDER BY triggered_at DESC
            LIMIT 1;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserGeofenceEvent>(sql, new { UserId = userId, GeofenceId = geofenceId });
    }

    public async Task AddAsync(UserGeofenceEvent geofenceEvent)
    {
        const string sql = @"
            INSERT INTO user_geofence_events (id, user_id, geofence_id, event_type, location_id, triggered_at, metadata)
            VALUES (@Id, @UserId, @GeofenceId, @EventType, @LocationId, @TriggeredAt, @Metadata::jsonb);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, geofenceEvent);
    }

    public async Task DeleteOldEventsAsync(int daysToKeep)
    {
        const string sql = "DELETE FROM user_geofence_events WHERE triggered_at < @CutoffDate;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { CutoffDate = DateTime.UtcNow.AddDays(-daysToKeep) });
    }
}
