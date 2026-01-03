using System.Text.Json;

namespace UserService.Domain.Entities;

public class Referral
{
    public Guid Id { get; private set; }
    public Guid ReferrerUserId { get; private set; }
    public Guid? ReferredUserId { get; private set; }
    public Guid ReferralCodeId { get; private set; }
    public string ReferralCode { get; private set; } = default!;
    public string Status { get; private set; } = "pending";
    public string? ReferredEmail { get; private set; }
    public string? ReferredPhone { get; private set; }
    public int ReferrerRewardPoints { get; private set; }
    public int ReferredRewardPoints { get; private set; }
    public bool ReferrerRewarded { get; private set; }
    public bool ReferredRewarded { get; private set; }
    public string CompletionRequirements { get; private set; } = "{}";
    public string CompletedRequirements { get; private set; } = "{}";
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected Referral() { }

    public Referral(
        Guid referrerUserId,
        Guid referralCodeId,
        string referralCode,
        string? referredEmail = null,
        string? referredPhone = null,
        int expirationDays = 30)
    {
        Id = Guid.NewGuid();
        ReferrerUserId = referrerUserId;
        ReferralCodeId = referralCodeId;
        ReferralCode = referralCode;
        Status = "pending";
        ReferredEmail = referredEmail;
        ReferredPhone = referredPhone;
        ReferrerRewardPoints = 0;
        ReferredRewardPoints = 0;
        ReferrerRewarded = false;
        ReferredRewarded = false;
        CompletionRequirements = JsonSerializer.Serialize(GetDefaultRequirements());
        CompletedRequirements = "{}";
        ExpiresAt = DateTime.UtcNow.AddDays(expirationDays);
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsRegistered(Guid referredUserId)
    {
        ReferredUserId = referredUserId;
        Status = "registered";
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsCompleted(int referrerPoints, int referredPoints)
    {
        Status = "completed";
        ReferrerRewardPoints = referrerPoints;
        ReferredRewardPoints = referredPoints;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkReferrerRewarded()
    {
        ReferrerRewarded = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkReferredRewarded()
    {
        ReferredRewarded = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsExpired()
    {
        Status = "expired";
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsCancelled()
    {
        Status = "cancelled";
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsExpired() => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

    public void UpdateCompletedRequirements(Dictionary<string, bool> completed)
    {
        CompletedRequirements = JsonSerializer.Serialize(completed);
        UpdatedAt = DateTime.UtcNow;
    }

    public Dictionary<string, bool> GetCompletionRequirementsDict()
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, bool>>(CompletionRequirements) ?? new(); }
        catch { return new(); }
    }

    public Dictionary<string, bool> GetCompletedRequirementsDict()
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, bool>>(CompletedRequirements) ?? new(); }
        catch { return new(); }
    }

    private static Dictionary<string, bool> GetDefaultRequirements() => new()
    {
        ["email_verified"] = false,
        ["first_review"] = false
    };
}
