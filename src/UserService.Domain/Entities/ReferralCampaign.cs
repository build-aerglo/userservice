namespace UserService.Domain.Entities;

public class ReferralCampaign
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public int BonusReferrerPoints { get; private set; }
    public int BonusReferredPoints { get; private set; }
    public decimal Multiplier { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public int? MaxReferralsPerUser { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected ReferralCampaign() { }

    public ReferralCampaign(
        string name,
        DateTime startsAt,
        DateTime endsAt,
        string? description = null,
        int bonusReferrerPoints = 0,
        int bonusReferredPoints = 0,
        decimal multiplier = 1.00m,
        int? maxReferralsPerUser = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        BonusReferrerPoints = bonusReferrerPoints;
        BonusReferredPoints = bonusReferredPoints;
        Multiplier = multiplier;
        StartsAt = startsAt;
        EndsAt = endsAt;
        MaxReferralsPerUser = maxReferralsPerUser;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsCurrentlyActive()
    {
        var now = DateTime.UtcNow;
        return IsActive && now >= StartsAt && now <= EndsAt;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
