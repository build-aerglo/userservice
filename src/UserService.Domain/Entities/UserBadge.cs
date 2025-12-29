namespace UserService.Domain.Entities;

/// <summary>
/// Represents a badge earned by a consumer user.
/// Badges are visual indicators of user activity, tenure, and contribution level.
/// </summary>
public class UserBadge
{
    protected UserBadge() { }

    public UserBadge(
        Guid userId,
        string badgeType,
        string? location = null,
        string? category = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        BadgeType = badgeType;
        Location = location;
        Category = category;
        EarnedAt = DateTime.UtcNow;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>
    /// Badge type: pioneer, top_contributor, expert_category, most_helpful, newbie, expert, pro
    /// </summary>
    public string BadgeType { get; private set; } = default!;

    /// <summary>
    /// Location for location-based badges (e.g., "Lagos" for Top Contributor in Lagos)
    /// </summary>
    public string? Location { get; private set; }

    /// <summary>
    /// Category for category-based badges (e.g., "Restaurants" for Expert in Restaurants)
    /// </summary>
    public string? Category { get; private set; }

    public DateTime EarnedAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Defines the types of badges available in the system
/// </summary>
public static class BadgeTypes
{
    // Achievement badges
    public const string Pioneer = "pioneer";           // Joined first 100 days of launch
    public const string TopContributor = "top_contributor"; // Top 5 reviewers in location
    public const string ExpertCategory = "expert_category"; // 10+ reviews in category
    public const string MostHelpful = "most_helpful";  // Top 10% by helpful votes

    // Tier badges (mutually exclusive)
    public const string Newbie = "newbie";   // <100 days OR <25 reviews
    public const string Expert = "expert";   // 100-250 days OR 25-50 reviews
    public const string Pro = "pro";         // 250+ days OR 50+ reviews
}
