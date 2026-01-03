using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class ReferralCampaignRepository : IReferralCampaignRepository
{
    private readonly string _connectionString;

    public ReferralCampaignRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<ReferralCampaign?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM referral_campaigns WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ReferralCampaign>(sql, new { Id = id });
    }

    public async Task<IEnumerable<ReferralCampaign>> GetAllAsync()
    {
        const string sql = "SELECT * FROM referral_campaigns ORDER BY starts_at DESC;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<ReferralCampaign>(sql);
    }

    public async Task<IEnumerable<ReferralCampaign>> GetActiveAsync()
    {
        const string sql = "SELECT * FROM referral_campaigns WHERE is_active = true ORDER BY starts_at DESC;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<ReferralCampaign>(sql);
    }

    public async Task<ReferralCampaign?> GetCurrentlyActiveAsync()
    {
        const string sql = @"
            SELECT * FROM referral_campaigns
            WHERE is_active = true AND starts_at <= @Now AND ends_at >= @Now
            ORDER BY multiplier DESC
            LIMIT 1;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ReferralCampaign>(sql, new { Now = DateTime.UtcNow });
    }

    public async Task AddAsync(ReferralCampaign campaign)
    {
        const string sql = @"
            INSERT INTO referral_campaigns (id, name, description, bonus_referrer_points, bonus_referred_points, multiplier, starts_at, ends_at, max_referrals_per_user, is_active, created_at)
            VALUES (@Id, @Name, @Description, @BonusReferrerPoints, @BonusReferredPoints, @Multiplier, @StartsAt, @EndsAt, @MaxReferralsPerUser, @IsActive, @CreatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, campaign);
    }

    public async Task UpdateAsync(ReferralCampaign campaign)
    {
        const string sql = @"
            UPDATE referral_campaigns SET
                name = @Name,
                description = @Description,
                bonus_referrer_points = @BonusReferrerPoints,
                bonus_referred_points = @BonusReferredPoints,
                multiplier = @Multiplier,
                starts_at = @StartsAt,
                ends_at = @EndsAt,
                max_referrals_per_user = @MaxReferralsPerUser,
                is_active = @IsActive
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, campaign);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM referral_campaigns WHERE id = @Id;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}
