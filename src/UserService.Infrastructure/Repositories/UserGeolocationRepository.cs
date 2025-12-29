using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class UserGeolocationRepository : IUserGeolocationRepository
{
    private readonly string _connectionString;

    public UserGeolocationRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserGeolocation?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM user_geolocations WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserGeolocation>(sql, new { Id = id });
    }

    public async Task<UserGeolocation?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_geolocations WHERE user_id = @UserId;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserGeolocation>(sql, new { UserId = userId });
    }

    public async Task AddAsync(UserGeolocation geolocation)
    {
        const string sql = @"
            INSERT INTO user_geolocations (id, user_id, latitude, longitude, state, lga, city, is_enabled, last_updated, created_at, updated_at)
            VALUES (@Id, @UserId, @Latitude, @Longitude, @State, @Lga, @City, @IsEnabled, @LastUpdated, @CreatedAt, @UpdatedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, geolocation);
    }

    public async Task UpdateAsync(UserGeolocation geolocation)
    {
        const string sql = @"
            UPDATE user_geolocations
            SET latitude = @Latitude,
                longitude = @Longitude,
                state = @State,
                lga = @Lga,
                city = @City,
                is_enabled = @IsEnabled,
                last_updated = @LastUpdated,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, geolocation);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM user_geolocations WHERE id = @Id;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<IEnumerable<UserGeolocation>> GetByStateAsync(string state, int limit = 100, int offset = 0)
    {
        const string sql = "SELECT * FROM user_geolocations WHERE state = @State AND is_enabled = true ORDER BY last_updated DESC LIMIT @Limit OFFSET @Offset;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<UserGeolocation>(sql, new { State = state, Limit = limit, Offset = offset });
    }

    public async Task<IEnumerable<UserGeolocation>> GetByLgaAsync(string lga, int limit = 100, int offset = 0)
    {
        const string sql = "SELECT * FROM user_geolocations WHERE lga = @Lga AND is_enabled = true ORDER BY last_updated DESC LIMIT @Limit OFFSET @Offset;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<UserGeolocation>(sql, new { Lga = lga, Limit = limit, Offset = offset });
    }

    public async Task<int> GetUserCountByStateAsync(string state)
    {
        const string sql = "SELECT COUNT(*) FROM user_geolocations WHERE state = @State AND is_enabled = true;";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { State = state });
    }
}

public class GeolocationHistoryRepository : IGeolocationHistoryRepository
{
    private readonly string _connectionString;

    public GeolocationHistoryRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<GeolocationHistory?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM geolocation_history WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<GeolocationHistory>(sql, new { Id = id });
    }

    public async Task<IEnumerable<GeolocationHistory>> GetByUserIdAsync(Guid userId, int limit = 50, int offset = 0)
    {
        const string sql = "SELECT * FROM geolocation_history WHERE user_id = @UserId ORDER BY recorded_at DESC LIMIT @Limit OFFSET @Offset;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<GeolocationHistory>(sql, new { UserId = userId, Limit = limit, Offset = offset });
    }

    public async Task AddAsync(GeolocationHistory history)
    {
        const string sql = @"
            INSERT INTO geolocation_history (id, user_id, latitude, longitude, state, lga, city, source, vpn_detected, recorded_at)
            VALUES (@Id, @UserId, @Latitude, @Longitude, @State, @Lga, @City, @Source, @VpnDetected, @RecordedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, history);
    }

    public async Task<IEnumerable<GeolocationHistory>> GetByUserIdAndDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        const string sql = "SELECT * FROM geolocation_history WHERE user_id = @UserId AND recorded_at >= @StartDate AND recorded_at <= @EndDate ORDER BY recorded_at DESC;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<GeolocationHistory>(sql, new { UserId = userId, StartDate = startDate, EndDate = endDate });
    }

    public async Task<GeolocationHistory?> GetLatestByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM geolocation_history WHERE user_id = @UserId ORDER BY recorded_at DESC LIMIT 1;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<GeolocationHistory>(sql, new { UserId = userId });
    }

    public async Task<int> GetVpnDetectionCountAsync(Guid userId)
    {
        const string sql = "SELECT COUNT(*) FROM geolocation_history WHERE user_id = @UserId AND vpn_detected = true;";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task DeleteOldHistoryAsync(DateTime cutoffDate)
    {
        const string sql = "DELETE FROM geolocation_history WHERE recorded_at < @CutoffDate;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { CutoffDate = cutoffDate });
    }
}
