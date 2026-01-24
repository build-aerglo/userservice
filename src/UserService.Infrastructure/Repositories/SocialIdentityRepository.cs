using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class SocialIdentityRepository : ISocialIdentityRepository
{
    private readonly string _connectionString;

    public SocialIdentityRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<SocialIdentity?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM social_identities WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<SocialIdentity>(sql, new { Id = id });
    }

    public async Task<SocialIdentity?> GetByProviderUserIdAsync(string provider, string providerUserId)
    {
        const string sql = @"
            SELECT * FROM social_identities
            WHERE provider = @Provider AND provider_user_id = @ProviderUserId;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<SocialIdentity>(sql, new { Provider = provider, ProviderUserId = providerUserId });
    }

    public async Task<SocialIdentity?> GetByUserAndProviderAsync(Guid userId, string provider)
    {
        const string sql = @"
            SELECT * FROM social_identities
            WHERE user_id = @UserId AND provider = @Provider;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<SocialIdentity>(sql, new { UserId = userId, Provider = provider });
    }

    public async Task<IEnumerable<SocialIdentity>> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM social_identities WHERE user_id = @UserId ORDER BY created_at DESC;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<SocialIdentity>(sql, new { UserId = userId });
    }

    public async Task AddAsync(SocialIdentity socialIdentity)
    {
        const string sql = @"
            INSERT INTO social_identities (
                id, user_id, provider, provider_user_id, email, name,
                access_token, refresh_token, token_expires_at, created_at, updated_at
            )
            VALUES (
                @Id, @UserId, @Provider, @ProviderUserId, @Email, @Name,
                @AccessToken, @RefreshToken, @TokenExpiresAt, @CreatedAt, @UpdatedAt
            )
            ON CONFLICT (user_id, provider) DO UPDATE
            SET email = EXCLUDED.email,
                name = EXCLUDED.name,
                access_token = EXCLUDED.access_token,
                refresh_token = EXCLUDED.refresh_token,
                token_expires_at = EXCLUDED.token_expires_at,
                updated_at = EXCLUDED.updated_at;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, socialIdentity);
    }

    public async Task UpdateAsync(SocialIdentity socialIdentity)
    {
        const string sql = @"
            UPDATE social_identities
            SET email = @Email,
                name = @Name,
                access_token = @AccessToken,
                refresh_token = @RefreshToken,
                token_expires_at = @TokenExpiresAt,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, socialIdentity);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM social_identities WHERE id = @Id;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }

    public async Task DeleteByUserAndProviderAsync(Guid userId, string provider)
    {
        const string sql = "DELETE FROM social_identities WHERE user_id = @UserId AND provider = @Provider;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { UserId = userId, Provider = provider });
    }
}