using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class UserLocationPreferencesRepository : IUserLocationPreferencesRepository
{
    private readonly string _connectionString;

    public UserLocationPreferencesRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserLocationPreferences?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_location_preferences WHERE user_id = @UserId;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserLocationPreferences>(sql, new { UserId = userId });
    }

    public async Task AddAsync(UserLocationPreferences preferences)
    {
        const string sql = @"
            INSERT INTO user_location_preferences (user_id, location_sharing_enabled, share_with_businesses, share_precise_location, location_history_enabled, max_history_days, auto_detect_timezone, default_search_radius_km, updated_at)
            VALUES (@UserId, @LocationSharingEnabled, @ShareWithBusinesses, @SharePreciseLocation, @LocationHistoryEnabled, @MaxHistoryDays, @AutoDetectTimezone, @DefaultSearchRadiusKm, @UpdatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, preferences);
    }

    public async Task UpdateAsync(UserLocationPreferences preferences)
    {
        const string sql = @"
            UPDATE user_location_preferences SET
                location_sharing_enabled = @LocationSharingEnabled,
                share_with_businesses = @ShareWithBusinesses,
                share_precise_location = @SharePreciseLocation,
                location_history_enabled = @LocationHistoryEnabled,
                max_history_days = @MaxHistoryDays,
                auto_detect_timezone = @AutoDetectTimezone,
                default_search_radius_km = @DefaultSearchRadiusKm,
                updated_at = @UpdatedAt
            WHERE user_id = @UserId;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, preferences);
    }

    public async Task UpsertAsync(UserLocationPreferences preferences)
    {
        const string sql = @"
            INSERT INTO user_location_preferences (user_id, location_sharing_enabled, share_with_businesses, share_precise_location, location_history_enabled, max_history_days, auto_detect_timezone, default_search_radius_km, updated_at)
            VALUES (@UserId, @LocationSharingEnabled, @ShareWithBusinesses, @SharePreciseLocation, @LocationHistoryEnabled, @MaxHistoryDays, @AutoDetectTimezone, @DefaultSearchRadiusKm, @UpdatedAt)
            ON CONFLICT (user_id) DO UPDATE SET
                location_sharing_enabled = EXCLUDED.location_sharing_enabled,
                share_with_businesses = EXCLUDED.share_with_businesses,
                share_precise_location = EXCLUDED.share_precise_location,
                location_history_enabled = EXCLUDED.location_history_enabled,
                max_history_days = EXCLUDED.max_history_days,
                auto_detect_timezone = EXCLUDED.auto_detect_timezone,
                default_search_radius_km = EXCLUDED.default_search_radius_km,
                updated_at = EXCLUDED.updated_at;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, preferences);
    }
}
