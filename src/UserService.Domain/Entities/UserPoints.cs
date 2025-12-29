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
        LastActivityDate = null;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal TotalPoints { get; private set; }
    public int CurrentStreak { get; private set; }
    public int LongestStreak { get; private set; }
    public DateTime? LastActivityDate { get; private set; }
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

    public void UpdateStreak(DateTime activityDate)
    {
        var today = activityDate.Date;

        if (LastActivityDate.HasValue)
        {
            var lastDate = LastActivityDate.Value.Date;
            var daysDiff = (today - lastDate).Days;

            if (daysDiff == 1)
            {
                // Consecutive day - increment streak
                CurrentStreak++;
            }
            else if (daysDiff > 1)
            {
                // Streak broken - reset
                CurrentStreak = 1;
            }
            // If daysDiff == 0, same day - no change to streak
        }
        else
        {
            // First activity ever
            CurrentStreak = 1;
        }

        // Update longest streak if necessary
        if (CurrentStreak > LongestStreak)
        {
            LongestStreak = CurrentStreak;
        }

        LastActivityDate = today;
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
    /// Type: earn, deduct, bonus, milestone
    /// </summary>
    public string TransactionType { get; private set; } = default!;

    public string Description { get; private set; } = default!;

    /// <summary>
    /// Reference to the related entity (e.g., review ID, referral ID)
    /// </summary>
    public Guid? ReferenceId { get; private set; }

    /// <summary>
    /// Type of reference: review, referral, streak, milestone
    /// </summary>
    public string? ReferenceType { get; private set; }

    public DateTime CreatedAt { get; private set; }
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
}

public static class ReferenceTypes
{
    public const string Review = "review";
    public const string Referral = "referral";
    public const string Streak = "streak";
    public const string Milestone = "milestone";
    public const string HelpfulVote = "helpful_vote";
}
