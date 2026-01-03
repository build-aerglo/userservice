namespace UserService.Domain.Entities;

public class ReferralRewardTier
{
    public Guid Id { get; private set; }
    public string TierName { get; private set; } = default!;
    public int MinReferrals { get; private set; }
    public int? MaxReferrals { get; private set; }
    public int ReferrerPoints { get; private set; }
    public int ReferredPoints { get; private set; }
    public decimal BonusMultiplier { get; private set; }
    public string AdditionalRewards { get; private set; } = "{}";
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected ReferralRewardTier() { }

    public ReferralRewardTier(
        string tierName,
        int minReferrals,
        int? maxReferrals,
        int referrerPoints,
        int referredPoints,
        decimal bonusMultiplier = 1.00m,
        string? additionalRewards = null)
    {
        Id = Guid.NewGuid();
        TierName = tierName;
        MinReferrals = minReferrals;
        MaxReferrals = maxReferrals;
        ReferrerPoints = referrerPoints;
        ReferredPoints = referredPoints;
        BonusMultiplier = bonusMultiplier;
        AdditionalRewards = additionalRewards ?? "{}";
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public bool AppliesToReferralCount(int successfulReferrals)
    {
        if (successfulReferrals < MinReferrals) return false;
        if (MaxReferrals.HasValue && successfulReferrals > MaxReferrals.Value) return false;
        return true;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
