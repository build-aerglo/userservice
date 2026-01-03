namespace UserService.Domain.Entities;

public class UserReferralCode
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string ReferralCode { get; private set; } = default!;
    public string? CustomCode { get; private set; }
    public bool IsActive { get; private set; }
    public int TotalReferrals { get; private set; }
    public int SuccessfulReferrals { get; private set; }
    public int PendingReferrals { get; private set; }
    public int TotalPointsEarned { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected UserReferralCode() { }

    public UserReferralCode(Guid userId, string? customCode = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ReferralCode = GenerateReferralCode();
        CustomCode = customCode?.ToUpperInvariant();
        IsActive = true;
        TotalReferrals = 0;
        SuccessfulReferrals = 0;
        PendingReferrals = 0;
        TotalPointsEarned = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public string ActiveCode => CustomCode ?? ReferralCode;

    public void SetCustomCode(string? customCode)
    {
        CustomCode = customCode?.ToUpperInvariant();
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordReferral()
    {
        TotalReferrals++;
        PendingReferrals++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CompleteReferral(int pointsEarned)
    {
        if (PendingReferrals > 0)
        {
            PendingReferrals--;
            SuccessfulReferrals++;
            TotalPointsEarned += pointsEarned;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void CancelReferral()
    {
        if (PendingReferrals > 0)
        {
            PendingReferrals--;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }

    private static string GenerateReferralCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
