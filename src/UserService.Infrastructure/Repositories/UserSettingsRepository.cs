using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class UserSettingsRepository : IUserSettingsRepository
{
    private readonly string _connectionString;

    public UserSettingsRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<UserSettings?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM user_settings WHERE user_id = @UserId;";
        await using var conn = CreateConnection();
        var result = await conn.QueryFirstOrDefaultAsync<UserSettings>(sql, new { UserId = userId });
        return result;
    }

    public async Task AddAsync(UserSettings userSettings)
    {
        const string sql = @"
            INSERT INTO user_settings (
                user_id, notification_preferences, dark_mode, created_at, updated_at
            )
            VALUES (
                @UserId, @NotificationPreferences::jsonb, @DarkMode, @CreatedAt, @UpdatedAt
            );";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, userSettings);
    }

    public async Task UpdateAsync(UserSettings userSettings)
    {
        const string sql = @"
            UPDATE user_settings
            SET 
                notification_preferences = @NotificationPreferences::jsonb,
                dark_mode = @DarkMode,
                updated_at = @UpdatedAt
            WHERE user_id = @UserId;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, userSettings);
    }

    public async Task DeleteAsync(Guid userId)
    {
        const string sql = "DELETE FROM user_settings WHERE user_id = @UserId;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { UserId = userId });
    }
}