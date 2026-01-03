using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class UserSavedLocationRepository : IUserSavedLocationRepository
{
    private readonly string _connectionString;

    public UserSavedLocationRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserSavedLocation?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM user_saved_locations WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserSavedLocation>(sql, new { Id = id });
    }

    public async Task<UserSavedLocation?> GetByUserIdAndNameAsync(Guid userId, string name)
    {
        const string sql = "SELECT * FROM user_saved_locations WHERE user_id = @UserId AND name = @Name;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserSavedLocation>(sql, new { UserId = userId, Name = name });
    }

    public async Task<IEnumerable<UserSavedLocation>> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_saved_locations WHERE user_id = @UserId ORDER BY is_default DESC, name;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<UserSavedLocation>(sql, new { UserId = userId });
    }

    public async Task<UserSavedLocation?> GetDefaultByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_saved_locations WHERE user_id = @UserId AND is_default = true LIMIT 1;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserSavedLocation>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<UserSavedLocation>> GetActiveByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_saved_locations WHERE user_id = @UserId AND is_active = true ORDER BY is_default DESC, name;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<UserSavedLocation>(sql, new { UserId = userId });
    }

    public async Task AddAsync(UserSavedLocation location)
    {
        const string sql = @"
            INSERT INTO user_saved_locations (id, user_id, name, label, latitude, longitude, address, city, state, country, country_code, postal_code, is_default, is_active, created_at, updated_at)
            VALUES (@Id, @UserId, @Name, @Label, @Latitude, @Longitude, @Address, @City, @State, @Country, @CountryCode, @PostalCode, @IsDefault, @IsActive, @CreatedAt, @UpdatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, location);
    }

    public async Task UpdateAsync(UserSavedLocation location)
    {
        const string sql = @"
            UPDATE user_saved_locations SET
                name = @Name,
                label = @Label,
                latitude = @Latitude,
                longitude = @Longitude,
                address = @Address,
                city = @City,
                state = @State,
                country = @Country,
                country_code = @CountryCode,
                postal_code = @PostalCode,
                is_default = @IsDefault,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, location);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM user_saved_locations WHERE id = @Id;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task ClearDefaultForUserAsync(Guid userId)
    {
        const string sql = "UPDATE user_saved_locations SET is_default = false, updated_at = @Now WHERE user_id = @UserId AND is_default = true;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { UserId = userId, Now = DateTime.UtcNow });
    }
}
