namespace UserService.Domain.Exceptions;

public class VerificationNotFoundException : Exception
{
    public VerificationNotFoundException(Guid userId, string type)
        : base($"{type} verification for user '{userId}' not found.") { }
}

public class VerificationExpiredException : Exception
{
    public VerificationExpiredException(string type)
        : base($"{type} verification has expired. Please request a new verification code.") { }
}

public class VerificationMaxAttemptsException : Exception
{
    public VerificationMaxAttemptsException(string type)
        : base($"Maximum verification attempts reached for {type}. Please request a new verification code.") { }
}

public class InvalidVerificationCodeException : Exception
{
    public InvalidVerificationCodeException(string type, int attemptsRemaining)
        : base($"Invalid {type} verification code. {attemptsRemaining} attempts remaining.") { }
}

public class AlreadyVerifiedException : Exception
{
    public AlreadyVerifiedException(Guid userId, string type)
        : base($"User '{userId}' has already verified their {type}.") { }
}

public class VerificationTokenNotFoundException : Exception
{
    public VerificationTokenNotFoundException(Guid token)
        : base($"Verification token '{token}' not found or expired.") { }
}
