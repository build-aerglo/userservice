namespace UserService.Domain.Exceptions;

public class BadgeNotFoundException : Exception
{
    public BadgeNotFoundException(string badgeName)
        : base($"Badge '{badgeName}' not found.") { }

    public BadgeNotFoundException(Guid badgeId)
        : base($"Badge with ID '{badgeId}' not found.") { }
}

public class BadgeAlreadyEarnedException : Exception
{
    public BadgeAlreadyEarnedException(Guid userId, string badgeName)
        : base($"User '{userId}' has already earned the badge '{badgeName}'.") { }
}

public class BadgeLevelNotFoundException : Exception
{
    public BadgeLevelNotFoundException(Guid userId)
        : base($"Badge level for user '{userId}' not found.") { }
}

public class InvalidBadgeTierException : Exception
{
    public InvalidBadgeTierException(int tier)
        : base($"Invalid badge tier: {tier}. Must be between 1 and 5.") { }
}
