using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class ReferralRewardTierRepository : IReferralRewardTierRepository
{
    private readonly string _connectionString;

    public ReferralRewardTierRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<ReferralRewardTier?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM referral_reward_tiers WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ReferralRewardTier>(sql, new { Id = id });
    }

    public async Task<IEnumerable<ReferralRewardTier>> GetAllAsync()
    {
        const string sql = "SELECT * FROM referral_reward_tiers ORDER BY min_referrals;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<ReferralRewardTier>(sql);
    }

    public async Task<IEnumerable<ReferralRewardTier>> GetActiveAsync()
    {
        const string sql = "SELECT * FROM referral_reward_tiers WHERE is_active = true ORDER BY min_referrals;";
        await using var conn = CreateConnection();
        return await conn.QueryAsync<ReferralRewardTier>(sql);
    }

    public async Task<ReferralRewardTier?> GetTierForReferralCountAsync(int referralCount)
    {
        const string sql = @"
            SELECT * FROM referral_reward_tiers
            WHERE is_active = true
                AND min_referrals <= @Count
                AND (max_referrals IS NULL OR max_referrals >= @Count)
            ORDER BY min_referrals DESC
            LIMIT 1;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<ReferralRewardTier>(sql, new { Count = referralCount });
    }

    public async Task AddAsync(ReferralRewardTier tier)
    {
        const string sql = @"
            INSERT INTO referral_reward_tiers (id, tier_name, min_referrals, max_referrals, referrer_points, referred_points, bonus_multiplier, additional_rewards, is_active, created_at)
            VALUES (@Id, @TierName, @MinReferrals, @MaxReferrals, @ReferrerPoints, @ReferredPoints, @BonusMultiplier, @AdditionalRewards::jsonb, @IsActive, @CreatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, tier);
    }

    public async Task UpdateAsync(ReferralRewardTier tier)
    {
        const string sql = @"
            UPDATE referral_reward_tiers SET
                tier_name = @TierName,
                min_referrals = @MinReferrals,
                max_referrals = @MaxReferrals,
                referrer_points = @ReferrerPoints,
                referred_points = @ReferredPoints,
                bonus_multiplier = @BonusMultiplier,
                additional_rewards = @AdditionalRewards::jsonb,
                is_active = @IsActive
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, tier);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM referral_reward_tiers WHERE id = @Id;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}
