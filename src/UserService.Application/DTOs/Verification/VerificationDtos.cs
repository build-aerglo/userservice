namespace UserService.Application.DTOs.Verification;

// Response DTOs
public record VerificationStatusDto(
    Guid UserId,
    bool EmailVerified,
    DateTime? EmailVerifiedAt,
    bool PhoneVerified,
    DateTime? PhoneVerifiedAt,
    bool IdentityVerified,
    DateTime? IdentityVerifiedAt,
    string VerificationLevel
);

public record EmailVerificationDto(
    Guid Id,
    Guid UserId,
    string Email,
    bool IsVerified,
    DateTime? VerifiedAt,
    int AttemptsRemaining,
    DateTime ExpiresAt,
    DateTime CreatedAt
);

public record PhoneVerificationDto(
    Guid Id,
    Guid UserId,
    string PhoneNumber,
    string CountryCode,
    string VerificationMethod,
    bool IsVerified,
    DateTime? VerifiedAt,
    int AttemptsRemaining,
    DateTime ExpiresAt,
    DateTime CreatedAt
);

public record VerificationResultDto(
    bool Success,
    string Message,
    string? VerificationLevel
);

// Request DTOs
public record SendEmailVerificationDto(
    Guid UserId,
    string Email
);

public record VerifyEmailDto(
    Guid UserId,
    string Code
);

public record VerifyEmailByTokenDto(
    Guid Token
);

public record SendPhoneVerificationDto(
    Guid UserId,
    string PhoneNumber,
    string CountryCode = "+1",
    string VerificationMethod = "sms"
);

public record VerifyPhoneDto(
    Guid UserId,
    string Code
);

public record ResendVerificationDto(
    Guid UserId,
    string Type // "email" or "phone"
);
