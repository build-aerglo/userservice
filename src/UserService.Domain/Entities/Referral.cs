namespace UserService.Domain.Entities;

/// <summary>
/// Represents a user's referral code and referral statistics.
/// </summary>
public class UserReferralCode
{
    protected UserReferralCode() { }

    public UserReferralCode(Guid userId, string code)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Code = code.ToUpperInvariant();
        TotalReferrals = 0;
        SuccessfulReferrals = 0;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>
    /// Unique referral code (e.g., "AMAKA2025")
    /// </summary>
    public string Code { get; private set; } = default!;

    /// <summary>
    /// Total number of users who signed up with this code
    /// </summary>
    public int TotalReferrals { get; private set; }

    /// <summary>
    /// Number of referrals that completed qualification (3 approved reviews)
    /// </summary>
    public int SuccessfulReferrals { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void IncrementTotalReferrals()
    {
        TotalReferrals++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementSuccessfulReferrals()
    {
        SuccessfulReferrals++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a referral relationship between users.
/// Tracks the referral lifecycle from registration to qualification.
/// </summary>
public class Referral
{
    protected Referral() { }

    public Referral(
        Guid referrerId,
        Guid referredUserId,
        string referralCode)
    {
        Id = Guid.NewGuid();
        ReferrerId = referrerId;
        ReferredUserId = referredUserId;
        ReferralCode = referralCode.ToUpperInvariant();
        Status = ReferralStatuses.Registered;
        ApprovedReviewCount = 0;
        PointsAwarded = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// The user who referred (owns the referral code)
    /// </summary>
    public Guid ReferrerId { get; private set; }

    /// <summary>
    /// The user who was referred
    /// </summary>
    public Guid ReferredUserId { get; private set; }

    /// <summary>
    /// The referral code used
    /// </summary>
    public string ReferralCode { get; private set; } = default!;

    /// <summary>
    /// Status: registered, active, qualified, completed
    /// </summary>
    public string Status { get; private set; } = default!;

    /// <summary>
    /// Number of approved reviews by the referred user
    /// </summary>
    public int ApprovedReviewCount { get; private set; }

    /// <summary>
    /// Whether points have been awarded to the referrer
    /// </summary>
    public bool PointsAwarded { get; private set; }

    public DateTime? QualifiedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Increment the approved review count and update status accordingly
    /// </summary>
    public void IncrementApprovedReviewCount()
    {
        ApprovedReviewCount++;

        // Update status based on review count
        if (ApprovedReviewCount >= 1 && ApprovedReviewCount < 3)
        {
            Status = ReferralStatuses.Active;
        }
        else if (ApprovedReviewCount >= 3 && !PointsAwarded)
        {
            Status = ReferralStatuses.Qualified;
            QualifiedAt = DateTime.UtcNow;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark the referral as completed after points are awarded
    /// </summary>
    public void MarkAsCompleted()
    {
        Status = ReferralStatuses.Completed;
        PointsAwarded = true;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

public static class ReferralStatuses
{
    public const string Registered = "registered";  // Just signed up with code
    public const string Active = "active";          // Has 1-2 approved reviews
    public const string Qualified = "qualified";    // Has 3+ approved reviews, pending reward
    public const string Completed = "completed";    // Reward given to referrer
}
