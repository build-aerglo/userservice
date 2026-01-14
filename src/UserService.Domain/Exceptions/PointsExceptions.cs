namespace UserService.Domain.Exceptions;

public class UserPointsNotFoundException(Guid userId)
    : Exception($"Points record for user '{userId}' was not found.");

public class InsufficientPointsException(Guid userId, decimal required, decimal available)
    : Exception($"User '{userId}' has insufficient points. Required: {required}, Available: {available}.");

public class PointTransactionFailedException(string message)
    : Exception(message);

public class InvalidPointsAmountException(decimal amount)
    : Exception($"Invalid points amount: {amount}. Points must be positive.");
