namespace UserService.Domain.Entities;

public class UserVerificationStatus
{
    public Guid UserId { get; private set; }
    public bool EmailVerified { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }
    public bool PhoneVerified { get; private set; }
    public DateTime? PhoneVerifiedAt { get; private set; }
    public bool IdentityVerified { get; private set; }
    public DateTime? IdentityVerifiedAt { get; private set; }
    public string VerificationLevel { get; private set; } = "none";
    public DateTime UpdatedAt { get; private set; }

    protected UserVerificationStatus() { }

    public UserVerificationStatus(Guid userId)
    {
        UserId = userId;
        EmailVerified = false;
        PhoneVerified = false;
        IdentityVerified = false;
        VerificationLevel = "none";
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkEmailVerified()
    {
        EmailVerified = true;
        EmailVerifiedAt = DateTime.UtcNow;
        RecalculateLevel();
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPhoneVerified()
    {
        PhoneVerified = true;
        PhoneVerifiedAt = DateTime.UtcNow;
        RecalculateLevel();
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkIdentityVerified()
    {
        IdentityVerified = true;
        IdentityVerifiedAt = DateTime.UtcNow;
        RecalculateLevel();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnmarkEmailVerified()
    {
        EmailVerified = false;
        EmailVerifiedAt = null;
        RecalculateLevel();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnmarkPhoneVerified()
    {
        PhoneVerified = false;
        PhoneVerifiedAt = null;
        RecalculateLevel();
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateLevel()
    {
        VerificationLevel = (EmailVerified, PhoneVerified, IdentityVerified) switch
        {
            (true, true, true) => "trusted",
            (true, true, false) => "verified",
            (true, false, _) => "basic",
            _ => "none"
        };
    }

    public bool MeetsMinimumLevel(string requiredLevel)
    {
        var levels = new Dictionary<string, int>
        {
            ["none"] = 0,
            ["basic"] = 1,
            ["verified"] = 2,
            ["trusted"] = 3
        };

        return levels.GetValueOrDefault(VerificationLevel, 0) >= levels.GetValueOrDefault(requiredLevel, 0);
    }
}
