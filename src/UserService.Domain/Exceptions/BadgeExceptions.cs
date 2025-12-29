namespace UserService.Domain.Exceptions;

public class BadgeNotFoundException(Guid badgeId)
    : Exception($"Badge with ID '{badgeId}' was not found.");

public class BadgeAlreadyExistsException(Guid userId, string badgeType)
    : Exception($"User '{userId}' already has badge type '{badgeType}'.");

public class InvalidBadgeTypeException(string badgeType)
    : Exception($"Invalid badge type: '{badgeType}'.");

public class BadgeAssignmentFailedException(string message)
    : Exception(message);
