namespace UserService.Domain.Exceptions;

public class PointRuleNotFoundException : Exception
{
    public PointRuleNotFoundException(string actionType)
        : base($"Point rule for action '{actionType}' not found.") { }

    public PointRuleNotFoundException(Guid ruleId)
        : base($"Point rule with ID '{ruleId}' not found.") { }
}

public class InsufficientPointsException : Exception
{
    public InsufficientPointsException(Guid userId, int requested, int available)
        : base($"User '{userId}' has insufficient points. Requested: {requested}, Available: {available}.") { }
}

public class PointsLimitReachedException : Exception
{
    public PointsLimitReachedException(string actionType, string limitType)
        : base($"Points limit reached for action '{actionType}': {limitType}.") { }
}

public class PointsCooldownActiveException : Exception
{
    public PointsCooldownActiveException(string actionType, TimeSpan remainingTime)
        : base($"Cooldown active for action '{actionType}'. Try again in {remainingTime.TotalMinutes:F0} minutes.") { }
}

public class UserPointsNotFoundException : Exception
{
    public UserPointsNotFoundException(Guid userId)
        : base($"Points record for user '{userId}' not found.") { }
}
