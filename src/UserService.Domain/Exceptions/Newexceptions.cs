namespace UserService.Domain.Exceptions;

/// <summary>
/// Thrown when a user's account is blocked (too many failed attempts, admin action, etc.)
/// </summary>
public class AccountBlockedException : Exception
{
    public AccountBlockedException(string message) : base(message) { }
    public AccountBlockedException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a user tries to log in but hasn't verified their email yet.
/// </summary>
public class EmailNotVerifiedException : Exception
{
    public EmailNotVerifiedException(string message) : base(message) { }
    public EmailNotVerifiedException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when rate limits are exceeded (Auth0 429, brute-force protection, etc.)
/// </summary>
public class RateLimitExceededException : Exception
{
    public int RetryAfterSeconds { get; }

    public RateLimitExceededException(string message, int retryAfterSeconds)
        : base(message)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    public RateLimitExceededException(string message, int retryAfterSeconds, Exception innerException)
        : base(message, innerException)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}

/// <summary>
/// Thrown when a social login's email is already registered under a different account.
/// </summary>
public class EmailAlreadyRegisteredException : Exception
{
    public string Email { get; }

    public EmailAlreadyRegisteredException(string email)
        : base($"An account with email '{email}' already exists.")
    {
        Email = email;
    }

    public EmailAlreadyRegisteredException(string email, Exception innerException)
        : base($"An account with email '{email}' already exists.", innerException)
    {
        Email = email;
    }
}

/// <summary>
/// Thrown when a user tries to unlink their only remaining authentication method.
/// </summary>
public class CannotUnlinkLastAuthMethodException : Exception
{
    public CannotUnlinkLastAuthMethodException(string message) : base(message) { }
    public CannotUnlinkLastAuthMethodException(string message, Exception innerException) : base(message, innerException) { }
}