using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class UserLocationRepository : IUserLocationRepository
{
    private readonly string _connectionString;

    public UserLocationRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserLocation?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM user_locations WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserLocation>(sql, new { Id = id });
    }

    public async Task<UserLocation?> GetLatestByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_locations WHERE user_id = @UserId ORDER BY recorded_at DESC LIMIT 1;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserLocation>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<UserLocation>> GetByUserIdAsync(Guid userId, int limit = 100, int offset = 0)
    {
        const string sql = @"
            SELECT * FROM user_locations
            WHERE user_id = @UserId
            ORDER BY recorded_at DESC
            LIMIT @Limit OFFSET @Offset;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<UserLocation>(sql, new { UserId = userId, Limit = limit, Offset = offset });
    }

    public async Task<IEnumerable<UserLocation>> GetByUserIdAndDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        const string sql = @"
            SELECT * FROM user_locations
            WHERE user_id = @UserId AND recorded_at >= @StartDate AND recorded_at <= @EndDate
            ORDER BY recorded_at DESC;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<UserLocation>(sql, new { UserId = userId, StartDate = startDate, EndDate = endDate });
    }

    public async Task<IEnumerable<UserLocation>> GetNearbyUsersAsync(decimal latitude, decimal longitude, decimal radiusKm, int limit = 50)
    {
        const string sql = @"
            SELECT * FROM user_locations ul
            WHERE ul.id IN (
                SELECT DISTINCT ON (user_id) id
                FROM user_locations
                ORDER BY user_id, recorded_at DESC
            )
            AND calculate_distance_km(@Latitude, @Longitude, ul.latitude, ul.longitude) <= @RadiusKm
            LIMIT @Limit;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<UserLocation>(sql, new { Latitude = latitude, Longitude = longitude, RadiusKm = radiusKm, Limit = limit });
    }

    public async Task AddAsync(UserLocation location)
    {
        const string sql = @"
            INSERT INTO user_locations (id, user_id, latitude, longitude, accuracy, altitude, altitude_accuracy, heading, speed, source, address, city, state, country, country_code, postal_code, timezone, metadata, recorded_at, created_at)
            VALUES (@Id, @UserId, @Latitude, @Longitude, @Accuracy, @Altitude, @AltitudeAccuracy, @Heading, @Speed, @Source, @Address, @City, @State, @Country, @CountryCode, @PostalCode, @Timezone, @Metadata::jsonb, @RecordedAt, @CreatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, location);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM user_locations WHERE id = @Id;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task DeleteOldLocationsAsync(Guid userId, int daysToKeep)
    {
        const string sql = "DELETE FROM user_locations WHERE user_id = @UserId AND recorded_at < @CutoffDate;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { UserId = userId, CutoffDate = DateTime.UtcNow.AddDays(-daysToKeep) });
    }

    public async Task DeleteAllByUserIdAsync(Guid userId)
    {
        const string sql = "DELETE FROM user_locations WHERE user_id = @UserId;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { UserId = userId });
    }
}
