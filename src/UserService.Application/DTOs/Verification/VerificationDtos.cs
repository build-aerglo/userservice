namespace UserService.Application.DTOs.Verification;

// Response DTOs
public record UserVerificationStatusDto(
    Guid UserId,
    bool PhoneVerified,
    bool EmailVerified,
    string VerificationLevel,
    DateTime? PhoneVerifiedAt,
    DateTime? EmailVerifiedAt
);

public record SendOtpResponseDto(
    bool Success,
    string Message,
    DateTime ExpiresAt,
    int RemainingAttempts
);

public record VerifyOtpResponseDto(
    bool Success,
    string Message,
    bool PhoneVerified
);

public record SendEmailVerificationResponseDto(
    bool Success,
    string Message,
    DateTime ExpiresAt
);

public record VerifyEmailResponseDto(
    bool Success,
    string Message,
    bool EmailVerified
);

// Request DTOs
public record SendPhoneOtpDto(
    Guid UserId,
    string PhoneNumber
);

public record VerifyPhoneOtpDto(
    Guid UserId,
    string Otp
);

public record SendEmailVerificationDto(
    Guid UserId,
    string Email
);

public record VerifyEmailDto(
    Guid UserId,
    string Token
);
