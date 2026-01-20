namespace UserService.Domain.Entities;

/// <summary>
/// Represents a user's point balance and statistics.
/// Points are earned through reviews, referrals, and other engagement activities.
/// </summary>
public class UserPoints
{
    protected UserPoints() { }

    public UserPoints(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TotalPoints = 0;
        CurrentStreak = 0;
        LongestStreak = 0;
        LastLoginDate = null;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal TotalPoints { get; private set; }
    public int CurrentStreak { get; private set; }
    public int LongestStreak { get; private set; }
    public DateTime? LastLoginDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void AddPoints(decimal points)
    {
        TotalPoints += points;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DeductPoints(decimal points)
    {
        TotalPoints = Math.Max(0, TotalPoints - points);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLoginStreak(DateTime loginDate)
    {
        var today = loginDate.Date;

        if (LastLoginDate.HasValue)
        {
            var lastDate = LastLoginDate.Value.Date;
            var daysDiff = (today - lastDate).Days;

            if (daysDiff == 1)
            {
                // Consecutive day - increment streak
                CurrentStreak++;
            }
            else if (daysDiff >= 14)
            {
                // 14+ days gap - reset streak
                CurrentStreak = 1;
            }
            else if (daysDiff > 1 && daysDiff < 14)
            {
                // Gap but less than 14 days - reset to 1
                CurrentStreak = 1;
            }
            // If daysDiff == 0, same day - no change to streak
        }
        else
        {
            // First login ever
            CurrentStreak = 1;
        }

        // Update longest streak if necessary
        if (CurrentStreak > LongestStreak)
        {
            LongestStreak = CurrentStreak;
        }

        LastLoginDate = today;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetStreak()
    {
        CurrentStreak = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the user's tier based on total points
    /// </summary>
    public string GetTier()
    {
        return TotalPoints switch
        {
            >= 10000 => PointTiers.Platinum,
            >= 5000 => PointTiers.Gold,
            >= 1000 => PointTiers.Silver,
            _ => PointTiers.Bronze
        };
    }
}

/// <summary>
/// Represents a single point transaction/history entry
/// </summary>
public class PointTransaction
{
    protected PointTransaction() { }

    public PointTransaction(
        Guid userId,
        decimal points,
        string transactionType,
        string description,
        Guid? referenceId = null,
        string? referenceType = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Points = points;
        TransactionType = transactionType;
        Description = description;
        ReferenceId = referenceId;
        ReferenceType = referenceType;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Points { get; private set; }

    /// <summary>
    /// Type: earn, deduct, bonus, milestone, redeem
    /// </summary>
    public string TransactionType { get; private set; } = default!;

    public string Description { get; private set; } = default!;

    /// <summary>
    /// Reference to the related entity (e.g., review ID, referral ID, redemption ID)
    /// </summary>
    public Guid? ReferenceId { get; private set; }

    /// <summary>
    /// Type of reference: review, referral, streak, milestone, helpful_vote, redemption
    /// </summary>
    public string? ReferenceType { get; private set; }

    public DateTime CreatedAt { get; private set; }
}

/// <summary>
/// Point rule entity for storing and viewing point rules
/// </summary>
public class PointRule
{
    protected PointRule() { }

    public PointRule(
        string actionType,
        string description,
        decimal basePointsNonVerified,
        decimal basePointsVerified,
        string? conditions = null,
        Guid? createdBy = null)
    {
        Id = Guid.NewGuid();
        ActionType = actionType;
        Description = description;
        BasePointsNonVerified = basePointsNonVerified;
        BasePointsVerified = basePointsVerified;
        Conditions = conditions;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public string ActionType { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal BasePointsNonVerified { get; private set; }
    public decimal BasePointsVerified { get; private set; }
    public string? Conditions { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public void Update(
        string? description = null,
        decimal? basePointsNonVerified = null,
        decimal? basePointsVerified = null,
        string? conditions = null,
        bool? isActive = null,
        Guid? updatedBy = null)
    {
        if (description != null) Description = description;
        if (basePointsNonVerified.HasValue) BasePointsNonVerified = basePointsNonVerified.Value;
        if (basePointsVerified.HasValue) BasePointsVerified = basePointsVerified.Value;
        if (conditions != null) Conditions = conditions;
        if (isActive.HasValue) IsActive = isActive.Value;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Point multiplier for special events (e.g., 2x points weekend)
/// </summary>
public class PointMultiplier
{
    protected PointMultiplier() { }

    public PointMultiplier(
        string name,
        string description,
        decimal multiplier,
        DateTime startDate,
        DateTime endDate,
        string[]? actionTypes = null,
        Guid? createdBy = null)
    {
        if (multiplier <= 0)
            throw new ArgumentException("Multiplier must be positive", nameof(multiplier));
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date", nameof(endDate));

        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Multiplier = multiplier;
        ActionTypes = actionTypes;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal Multiplier { get; private set; }
    public string[]? ActionTypes { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public void Update(
        string? name = null,
        string? description = null,
        decimal? multiplier = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string[]? actionTypes = null,
        bool? isActive = null,
        Guid? updatedBy = null)
    {
        if (name != null) Name = name;
        if (description != null) Description = description;
        if (multiplier.HasValue)
        {
            if (multiplier.Value <= 0)
                throw new ArgumentException("Multiplier must be positive", nameof(multiplier));
            Multiplier = multiplier.Value;
        }
        if (startDate.HasValue) StartDate = startDate.Value;
        if (endDate.HasValue)
        {
            if (endDate.Value <= StartDate)
                throw new ArgumentException("End date must be after start date", nameof(endDate));
            EndDate = endDate.Value;
        }
        if (actionTypes != null) ActionTypes = actionTypes;
        if (isActive.HasValue) IsActive = isActive.Value;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsCurrentlyActive()
    {
        var now = DateTime.UtcNow;
        return IsActive && now >= StartDate && now <= EndDate;
    }
}

/// <summary>
/// Point redemption for airtime purchases
/// </summary>
public class PointRedemption
{
    protected PointRedemption() { }

    public PointRedemption(
        Guid userId,
        decimal pointsRedeemed,
        decimal amountInNaira,
        string phoneNumber,
        string status = "pending")
    {
        Id = Guid.NewGuid();
        UserId = userId;
        PointsRedeemed = pointsRedeemed;
        AmountInNaira = amountInNaira;
        PhoneNumber = phoneNumber;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal PointsRedeemed { get; private set; }
    public decimal AmountInNaira { get; private set; }
    public string PhoneNumber { get; private set; } = default!;
    public string Status { get; private set; } = default!; // pending, completed, failed
    public string? TransactionReference { get; private set; }
    public string? ProviderResponse { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public void MarkAsCompleted(string transactionReference, string? providerResponse = null)
    {
        Status = "COMPLETED";
        TransactionReference = transactionReference;
        ProviderResponse = providerResponse;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string? providerResponse = null)
    {
        Status = "FAILED";
        ProviderResponse = providerResponse;
        UpdatedAt = DateTime.UtcNow;
    }
}

public static class PointTiers
{
    public const string Bronze = "bronze";     // 0-999 points
    public const string Silver = "silver";     // 1,000-4,999 points
    public const string Gold = "gold";         // 5,000-9,999 points
    public const string Platinum = "platinum"; // 10,000+ points
}

public static class TransactionTypes
{
    public const string Earn = "earn";
    public const string Deduct = "deduct";
    public const string Bonus = "bonus";
    public const string Milestone = "milestone";
    public const string Redeem = "redeem";
}

public static class ReferenceTypes
{
    public const string Review = "review";
    public const string Referral = "referral";
    public const string Streak = "streak";
    public const string Milestone = "milestone";
    public const string HelpfulVote = "helpful_vote";
    public const string Redemption = "redemption";
}