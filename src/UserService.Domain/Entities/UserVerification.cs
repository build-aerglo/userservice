namespace UserService.Domain.Entities;

/// <summary>
/// Represents a user's verification status for phone and email.
/// Verified users earn 50% more points and get a verified badge.
/// </summary>
public class UserVerification
{
    protected UserVerification() { }

    public UserVerification(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        PhoneVerified = false;
        EmailVerified = false;
        PhoneVerifiedAt = null;
        EmailVerifiedAt = null;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public bool PhoneVerified { get; private set; }
    public bool EmailVerified { get; private set; }
    public DateTime? PhoneVerifiedAt { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void VerifyPhone()
    {
        PhoneVerified = true;
        PhoneVerifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void VerifyEmail()
    {
        EmailVerified = true;
        EmailVerifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnverifyPhone()
    {
        PhoneVerified = false;
        PhoneVerifiedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnverifyEmail()
    {
        EmailVerified = false;
        EmailVerifiedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the verification level: unverified, partial, fully_verified
    /// </summary>
    public string GetVerificationLevel()
    {
        if (PhoneVerified && EmailVerified)
            return VerificationLevels.FullyVerified;
        if (PhoneVerified || EmailVerified)
            return VerificationLevels.Partial;
        return VerificationLevels.Unverified;
    }

    /// <summary>
    /// Returns true if the user is at least partially verified
    /// </summary>
    public bool IsVerified => PhoneVerified || EmailVerified;

    /// <summary>
    /// Returns true if the user is fully verified (both phone and email)
    /// </summary>
    public bool IsFullyVerified => PhoneVerified && EmailVerified;
}

/// <summary>
/// Represents a verification code/token for phone (OTP) or email verification
/// </summary>
public class VerificationToken
{
    protected VerificationToken() { }

    public VerificationToken(
        Guid userId,
        string verificationType,
        string token,
        string target,
        int expiresInMinutes = 10)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        VerificationType = verificationType;
        Token = token;
        Target = target;
        Attempts = 0;
        MaxAttempts = 3;
        IsUsed = false;
        ExpiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes);
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>
    /// Type: phone, email
    /// </summary>
    public string VerificationType { get; private set; } = default!;

    /// <summary>
    /// The verification code/token
    /// </summary>
    public string Token { get; private set; } = default!;

    /// <summary>
    /// The target phone number or email address
    /// </summary>
    public string Target { get; private set; } = default!;

    public int Attempts { get; private set; }
    public int MaxAttempts { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool HasExceededMaxAttempts => Attempts >= MaxAttempts;
    public bool IsValid => !IsUsed && !IsExpired && !HasExceededMaxAttempts;

    public void IncrementAttempts()
    {
        Attempts++;
    }

    public void MarkAsUsed()
    {
        IsUsed = true;
    }
}

public static class VerificationLevels
{
    public const string Unverified = "unverified";
    public const string Partial = "partial";
    public const string FullyVerified = "fully_verified";
}

public static class VerificationTypes
{
    public const string Phone = "phone";
    public const string Email = "email";
}
