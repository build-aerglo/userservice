using UserService.Application.DTOs.Verification;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class VerificationService(
    IEmailVerificationRepository emailVerificationRepository,
    IPhoneVerificationRepository phoneVerificationRepository,
    IUserVerificationStatusRepository userVerificationStatusRepository,
    IUserRepository userRepository,
    IPointsService pointsService
) : IVerificationService
{
    public async Task<VerificationStatusDto> GetVerificationStatusAsync(Guid userId)
    {
        var status = await userVerificationStatusRepository.GetByUserIdAsync(userId);
        if (status == null)
            throw new VerificationNotFoundException(userId, "Verification status");
        return MapToStatusDto(status);
    }

    public async Task<VerificationStatusDto> GetOrCreateVerificationStatusAsync(Guid userId)
    {
        var status = await userVerificationStatusRepository.GetByUserIdAsync(userId);
        if (status == null)
        {
            status = new UserVerificationStatus(userId);
            await userVerificationStatusRepository.AddAsync(status);
        }
        return MapToStatusDto(status);
    }

    public async Task<EmailVerificationDto> SendEmailVerificationAsync(SendEmailVerificationDto dto)
    {
        // Check if already verified
        var status = await userVerificationStatusRepository.GetByUserIdAsync(dto.UserId);
        if (status?.EmailVerified == true)
            throw new AlreadyVerifiedException(dto.UserId, "email");

        // Check for existing active verification
        var existing = await emailVerificationRepository.GetActiveByUserIdAsync(dto.UserId);
        if (existing != null)
        {
            // Regenerate if expired or too many attempts
            if (existing.IsExpired() || !existing.HasAttemptsRemaining())
            {
                existing.Regenerate();
                await emailVerificationRepository.UpdateAsync(existing);
            }
            return MapToEmailDto(existing);
        }

        // Create new verification
        var verification = new EmailVerification(dto.UserId, dto.Email);
        await emailVerificationRepository.AddAsync(verification);

        // TODO: Send actual email with code/link
        // In production, integrate with email service (SendGrid, AWS SES, etc.)

        return MapToEmailDto(verification);
    }

    public async Task<VerificationResultDto> VerifyEmailAsync(VerifyEmailDto dto)
    {
        var verification = await emailVerificationRepository.GetActiveByUserIdAsync(dto.UserId);
        if (verification == null)
            throw new VerificationNotFoundException(dto.UserId, "Email");

        if (verification.IsExpired())
            throw new VerificationExpiredException("Email");

        if (!verification.HasAttemptsRemaining())
            throw new VerificationMaxAttemptsException("Email");

        if (!verification.Verify(dto.Code))
        {
            await emailVerificationRepository.UpdateAsync(verification);
            var remaining = verification.MaxAttempts - verification.Attempts;
            throw new InvalidVerificationCodeException("email", remaining);
        }

        await emailVerificationRepository.UpdateAsync(verification);

        // Update verification status
        var status = await userVerificationStatusRepository.GetByUserIdAsync(dto.UserId);
        if (status == null)
        {
            status = new UserVerificationStatus(dto.UserId);
            await userVerificationStatusRepository.AddAsync(status);
        }
        status.MarkEmailVerified();
        await userVerificationStatusRepository.UpdateAsync(status);

        // Award points for verification
        try
        {
            await pointsService.EarnPointsAsync(new DTOs.Points.EarnPointsDto(
                dto.UserId,
                "email_verified",
                "verification",
                dto.UserId,
                "Email verification completed"
            ));
        }
        catch { /* Points awarding is optional */ }

        return new VerificationResultDto(true, "Email verified successfully", status.VerificationLevel);
    }

    public async Task<VerificationResultDto> VerifyEmailByTokenAsync(VerifyEmailByTokenDto dto)
    {
        var verification = await emailVerificationRepository.GetByTokenAsync(dto.Token);
        if (verification == null)
            throw new VerificationTokenNotFoundException(dto.Token);

        if (verification.IsExpired())
            throw new VerificationExpiredException("Email");

        if (!verification.VerifyByToken(dto.Token))
            throw new VerificationTokenNotFoundException(dto.Token);

        await emailVerificationRepository.UpdateAsync(verification);

        // Update verification status
        var status = await userVerificationStatusRepository.GetByUserIdAsync(verification.UserId);
        if (status == null)
        {
            status = new UserVerificationStatus(verification.UserId);
            await userVerificationStatusRepository.AddAsync(status);
        }
        status.MarkEmailVerified();
        await userVerificationStatusRepository.UpdateAsync(status);

        return new VerificationResultDto(true, "Email verified successfully", status.VerificationLevel);
    }

    public async Task<EmailVerificationDto> ResendEmailVerificationAsync(Guid userId)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new EndUserNotFoundException(userId);

        var existing = await emailVerificationRepository.GetLatestByUserIdAsync(userId);
        if (existing != null)
        {
            existing.Regenerate();
            await emailVerificationRepository.UpdateAsync(existing);
            return MapToEmailDto(existing);
        }

        var verification = new EmailVerification(userId, user.Email);
        await emailVerificationRepository.AddAsync(verification);
        return MapToEmailDto(verification);
    }

    public async Task<EmailVerificationDto?> GetActiveEmailVerificationAsync(Guid userId)
    {
        var verification = await emailVerificationRepository.GetActiveByUserIdAsync(userId);
        return verification != null ? MapToEmailDto(verification) : null;
    }

    public async Task<PhoneVerificationDto> SendPhoneVerificationAsync(SendPhoneVerificationDto dto)
    {
        // Check if already verified
        var status = await userVerificationStatusRepository.GetByUserIdAsync(dto.UserId);
        if (status?.PhoneVerified == true)
            throw new AlreadyVerifiedException(dto.UserId, "phone");

        // Check for existing active verification
        var existing = await phoneVerificationRepository.GetActiveByUserIdAsync(dto.UserId);
        if (existing != null)
        {
            if (existing.IsExpired() || !existing.HasAttemptsRemaining())
            {
                existing.Regenerate();
                await phoneVerificationRepository.UpdateAsync(existing);
            }
            return MapToPhoneDto(existing);
        }

        // Create new verification
        var verification = new PhoneVerification(
            dto.UserId,
            dto.PhoneNumber,
            dto.CountryCode,
            dto.VerificationMethod
        );
        await phoneVerificationRepository.AddAsync(verification);

        // TODO: Send actual SMS/voice call with code
        // In production, integrate with SMS service (Twilio, AWS SNS, etc.)

        return MapToPhoneDto(verification);
    }

    public async Task<VerificationResultDto> VerifyPhoneAsync(VerifyPhoneDto dto)
    {
        var verification = await phoneVerificationRepository.GetActiveByUserIdAsync(dto.UserId);
        if (verification == null)
            throw new VerificationNotFoundException(dto.UserId, "Phone");

        if (verification.IsExpired())
            throw new VerificationExpiredException("Phone");

        if (!verification.HasAttemptsRemaining())
            throw new VerificationMaxAttemptsException("Phone");

        if (!verification.Verify(dto.Code))
        {
            await phoneVerificationRepository.UpdateAsync(verification);
            var remaining = verification.MaxAttempts - verification.Attempts;
            throw new InvalidVerificationCodeException("phone", remaining);
        }

        await phoneVerificationRepository.UpdateAsync(verification);

        // Update verification status
        var status = await userVerificationStatusRepository.GetByUserIdAsync(dto.UserId);
        if (status == null)
        {
            status = new UserVerificationStatus(dto.UserId);
            await userVerificationStatusRepository.AddAsync(status);
        }
        status.MarkPhoneVerified();
        await userVerificationStatusRepository.UpdateAsync(status);

        // Award points for verification
        try
        {
            await pointsService.EarnPointsAsync(new DTOs.Points.EarnPointsDto(
                dto.UserId,
                "phone_verified",
                "verification",
                dto.UserId,
                "Phone verification completed"
            ));
        }
        catch { /* Points awarding is optional */ }

        return new VerificationResultDto(true, "Phone verified successfully", status.VerificationLevel);
    }

    public async Task<PhoneVerificationDto> ResendPhoneVerificationAsync(Guid userId)
    {
        var existing = await phoneVerificationRepository.GetLatestByUserIdAsync(userId);
        if (existing == null)
            throw new VerificationNotFoundException(userId, "Phone");

        existing.Regenerate();
        await phoneVerificationRepository.UpdateAsync(existing);
        return MapToPhoneDto(existing);
    }

    public async Task<PhoneVerificationDto?> GetActivePhoneVerificationAsync(Guid userId)
    {
        var verification = await phoneVerificationRepository.GetActiveByUserIdAsync(userId);
        return verification != null ? MapToPhoneDto(verification) : null;
    }

    public async Task CleanupExpiredVerificationsAsync()
    {
        await emailVerificationRepository.DeleteExpiredAsync();
        await phoneVerificationRepository.DeleteExpiredAsync();
    }

    private static VerificationStatusDto MapToStatusDto(UserVerificationStatus s) => new(
        s.UserId,
        s.EmailVerified,
        s.EmailVerifiedAt,
        s.PhoneVerified,
        s.PhoneVerifiedAt,
        s.IdentityVerified,
        s.IdentityVerifiedAt,
        s.VerificationLevel
    );

    private static EmailVerificationDto MapToEmailDto(EmailVerification v) => new(
        v.Id,
        v.UserId,
        v.Email,
        v.IsVerified,
        v.VerifiedAt,
        v.MaxAttempts - v.Attempts,
        v.ExpiresAt,
        v.CreatedAt
    );

    private static PhoneVerificationDto MapToPhoneDto(PhoneVerification v) => new(
        v.Id,
        v.UserId,
        v.PhoneNumber,
        v.CountryCode,
        v.VerificationMethod,
        v.IsVerified,
        v.VerifiedAt,
        v.MaxAttempts - v.Attempts,
        v.ExpiresAt,
        v.CreatedAt
    );
}
