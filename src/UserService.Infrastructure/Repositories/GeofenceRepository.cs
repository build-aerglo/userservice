using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class GeofenceRepository : IGeofenceRepository
{
    private readonly string _connectionString;

    public GeofenceRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<Geofence?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM geofences WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Geofence>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Geofence>> GetAllAsync()
    {
        const string sql = "SELECT * FROM geofences ORDER BY name;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<Geofence>(sql);
    }

    public async Task<IEnumerable<Geofence>> GetActiveAsync()
    {
        const string sql = "SELECT * FROM geofences WHERE is_active = true ORDER BY name;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<Geofence>(sql);
    }

    public async Task<IEnumerable<Geofence>> GetNearbyAsync(decimal latitude, decimal longitude, decimal radiusKm)
    {
        const string sql = @"
            SELECT * FROM geofences
            WHERE is_active = true
            AND calculate_distance_km(@Latitude, @Longitude, latitude, longitude) <= @RadiusKm
            ORDER BY calculate_distance_km(@Latitude, @Longitude, latitude, longitude);";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<Geofence>(sql, new { Latitude = latitude, Longitude = longitude, RadiusKm = radiusKm });
    }

    public async Task<IEnumerable<Geofence>> GetContainingPointAsync(decimal latitude, decimal longitude)
    {
        const string sql = @"
            SELECT * FROM geofences
            WHERE is_active = true
            AND is_within_geofence(@Latitude, @Longitude, latitude, longitude, radius_meters)
            ORDER BY radius_meters;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<Geofence>(sql, new { Latitude = latitude, Longitude = longitude });
    }

    public async Task AddAsync(Geofence geofence)
    {
        const string sql = @"
            INSERT INTO geofences (id, name, description, latitude, longitude, radius_meters, geofence_type, polygon_points, trigger_on_enter, trigger_on_exit, trigger_on_dwell, dwell_time_seconds, is_active, metadata, created_at, updated_at)
            VALUES (@Id, @Name, @Description, @Latitude, @Longitude, @RadiusMeters, @GeofenceType, @PolygonPoints::jsonb, @TriggerOnEnter, @TriggerOnExit, @TriggerOnDwell, @DwellTimeSeconds, @IsActive, @Metadata::jsonb, @CreatedAt, @UpdatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, geofence);
    }

    public async Task UpdateAsync(Geofence geofence)
    {
        const string sql = @"
            UPDATE geofences SET
                name = @Name,
                description = @Description,
                latitude = @Latitude,
                longitude = @Longitude,
                radius_meters = @RadiusMeters,
                geofence_type = @GeofenceType,
                polygon_points = @PolygonPoints::jsonb,
                trigger_on_enter = @TriggerOnEnter,
                trigger_on_exit = @TriggerOnExit,
                trigger_on_dwell = @TriggerOnDwell,
                dwell_time_seconds = @DwellTimeSeconds,
                is_active = @IsActive,
                metadata = @Metadata::jsonb,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, geofence);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM geofences WHERE id = @Id;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}
