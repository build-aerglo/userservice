using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UserService.Application.DTOs.Verification;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class VerificationService(
    IUserVerificationRepository verificationRepository,
    IVerificationTokenRepository tokenRepository,
    IUserRepository userRepository
) : IVerificationService
{
    // Token expiration times
    private const int PhoneOtpExpirationMinutes = 10;
    private const int EmailTokenExpirationHours = 24;
    private const int MaxOtpAttempts = 3;

    // Verified user points multiplier
    private const decimal VerifiedMultiplier = 1.5m;
    private const decimal NonVerifiedMultiplier = 1.0m;

    // Nigerian phone number regex (+234XXXXXXXXXX or 0XXXXXXXXXX)
    private static readonly Regex NigerianPhoneRegex = new(
        @"^(\+234|234|0)[789][01]\d{8}$",
        RegexOptions.Compiled);

    public async Task<UserVerificationStatusDto> GetVerificationStatusAsync(Guid userId)
    {
        var verification = await verificationRepository.GetByUserIdAsync(userId);
        if (verification is null)
        {
            // Initialize verification for user if not exists
            await InitializeUserVerificationAsync(userId);
            verification = await verificationRepository.GetByUserIdAsync(userId);
        }

        return new UserVerificationStatusDto(
            UserId: userId,
            PhoneVerified: verification!.PhoneVerified,
            EmailVerified: verification.EmailVerified,
            VerificationLevel: verification.GetVerificationLevel(),
            PhoneVerifiedAt: verification.PhoneVerifiedAt,
            EmailVerifiedAt: verification.EmailVerifiedAt
        );
    }

    public async Task<SendOtpResponseDto> SendPhoneOtpAsync(SendPhoneOtpDto dto)
    {
        // Validate user exists
        var user = await userRepository.GetByIdAsync(dto.UserId);
        if (user is null)
            throw new EndUserNotFoundException(dto.UserId);

        // Validate phone number format
        if (!ValidateNigerianPhoneNumber(dto.PhoneNumber))
            throw new InvalidPhoneNumberException(dto.PhoneNumber);

        // Check if already verified
        var verification = await verificationRepository.GetByUserIdAsync(dto.UserId);
        if (verification?.PhoneVerified == true)
            throw new PhoneAlreadyVerifiedException(dto.UserId);

        // Invalidate any previous tokens
        await tokenRepository.InvalidatePreviousTokensAsync(dto.UserId, VerificationTypes.Phone);

        // Generate 6-digit OTP
        var otp = GenerateOtp();

        // Create token record
        var token = new VerificationToken(
            userId: dto.UserId,
            verificationType: VerificationTypes.Phone,
            token: otp,
            target: dto.PhoneNumber,
            expiresInMinutes: PhoneOtpExpirationMinutes
        );

        await tokenRepository.AddAsync(token);

        // In a real implementation, you would send the OTP via SMS here
        // For now, we'll just return the response
        // await _smsService.SendOtpAsync(dto.PhoneNumber, otp);

        return new SendOtpResponseDto(
            Success: true,
            Message: "OTP sent successfully",
            ExpiresAt: token.ExpiresAt,
            RemainingAttempts: MaxOtpAttempts
        );
    }

    public async Task<VerifyOtpResponseDto> VerifyPhoneOtpAsync(VerifyPhoneOtpDto dto)
    {
        // Get the latest token for this user
        var token = await tokenRepository.GetLatestByUserIdAndTypeAsync(dto.UserId, VerificationTypes.Phone);

        if (token is null)
            throw new InvalidVerificationTokenException("No verification code found. Please request a new one.");

        // Check if token is valid
        if (token.IsExpired)
            throw new VerificationTokenExpiredException();

        if (token.HasExceededMaxAttempts)
            throw new MaxVerificationAttemptsExceededException();

        if (token.IsUsed)
            throw new InvalidVerificationTokenException("This code has already been used.");

        // Increment attempts
        token.IncrementAttempts();
        await tokenRepository.UpdateAsync(token);

        // Check if OTP matches
        if (token.Token != dto.Otp)
        {
            return new VerifyOtpResponseDto(
                Success: false,
                Message: $"Invalid OTP. {MaxOtpAttempts - token.Attempts} attempts remaining.",
                PhoneVerified: false
            );
        }

        // Mark token as used
        token.MarkAsUsed();
        await tokenRepository.UpdateAsync(token);

        // Update verification status
        var verification = await verificationRepository.GetByUserIdAsync(dto.UserId);
        if (verification is null)
        {
            await InitializeUserVerificationAsync(dto.UserId);
            verification = await verificationRepository.GetByUserIdAsync(dto.UserId);
        }

        verification!.VerifyPhone();
        await verificationRepository.UpdateAsync(verification);

        return new VerifyOtpResponseDto(
            Success: true,
            Message: "Phone verified successfully",
            PhoneVerified: true
        );
    }

    public async Task<SendEmailVerificationResponseDto> SendEmailVerificationAsync(SendEmailVerificationDto dto)
    {
        // Validate user exists
        var user = await userRepository.GetByIdAsync(dto.UserId);
        if (user is null)
            throw new EndUserNotFoundException(dto.UserId);

        // Check if already verified
        var verification = await verificationRepository.GetByUserIdAsync(dto.UserId);
        if (verification?.EmailVerified == true)
            throw new EmailAlreadyVerifiedException(dto.UserId);

        // Invalidate any previous tokens
        await tokenRepository.InvalidatePreviousTokensAsync(dto.UserId, VerificationTypes.Email);

        // Generate email verification token
        var emailToken = GenerateEmailToken();

        // Create token record
        var token = new VerificationToken(
            userId: dto.UserId,
            verificationType: VerificationTypes.Email,
            token: emailToken,
            target: dto.Email,
            expiresInMinutes: EmailTokenExpirationHours * 60
        );

        await tokenRepository.AddAsync(token);

        // In a real implementation, you would send the verification email here
        // var verificationLink = $"{_config["FrontendUrl"]}/verify-email?token={emailToken}";
        // await _emailService.SendVerificationEmailAsync(dto.Email, verificationLink);

        return new SendEmailVerificationResponseDto(
            Success: true,
            Message: "Verification email sent successfully",
            ExpiresAt: token.ExpiresAt
        );
    }

    public async Task<VerifyEmailResponseDto> VerifyEmailAsync(VerifyEmailDto dto)
    {
        // Get token by token value
        var token = await tokenRepository.GetByTokenAsync(dto.Token);

        if (token is null)
            throw new InvalidVerificationTokenException("Invalid verification token.");

        if (token.UserId != dto.UserId)
            throw new InvalidVerificationTokenException("Token does not belong to this user.");

        if (token.IsExpired)
            throw new VerificationTokenExpiredException();

        if (token.IsUsed)
            throw new InvalidVerificationTokenException("This token has already been used.");

        // Mark token as used
        token.MarkAsUsed();
        await tokenRepository.UpdateAsync(token);

        // Update verification status
        var verification = await verificationRepository.GetByUserIdAsync(dto.UserId);
        if (verification is null)
        {
            await InitializeUserVerificationAsync(dto.UserId);
            verification = await verificationRepository.GetByUserIdAsync(dto.UserId);
        }

        verification!.VerifyEmail();
        await verificationRepository.UpdateAsync(verification);

        return new VerifyEmailResponseDto(
            Success: true,
            Message: "Email verified successfully",
            EmailVerified: true
        );
    }

    public async Task<bool> IsUserVerifiedAsync(Guid userId)
    {
        return await verificationRepository.IsUserVerifiedAsync(userId);
    }

    public async Task<bool> IsUserFullyVerifiedAsync(Guid userId)
    {
        return await verificationRepository.IsUserFullyVerifiedAsync(userId);
    }

    public async Task<string> GetVerificationLevelAsync(Guid userId)
    {
        var verification = await verificationRepository.GetByUserIdAsync(userId);
        return verification?.GetVerificationLevel() ?? VerificationLevels.Unverified;
    }

    public async Task<decimal> GetPointsMultiplierAsync(Guid userId)
    {
        var isVerified = await IsUserVerifiedAsync(userId);
        return isVerified ? VerifiedMultiplier : NonVerifiedMultiplier;
    }

    public async Task InitializeUserVerificationAsync(Guid userId)
    {
        var existingVerification = await verificationRepository.GetByUserIdAsync(userId);
        if (existingVerification is not null)
            return;

        var verification = new UserVerification(userId);
        await verificationRepository.AddAsync(verification);
    }

    public bool ValidateNigerianPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Remove spaces and dashes
        var cleaned = phoneNumber.Replace(" ", "").Replace("-", "");

        return NigerianPhoneRegex.IsMatch(cleaned);
    }

    public async Task CleanupExpiredTokensAsync()
    {
        await tokenRepository.DeleteExpiredTokensAsync();
    }

    private static string GenerateOtp()
    {
        // Generate 6-digit OTP using cryptographic random number generator
        var bytes = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        var value = BitConverter.ToUInt32(bytes, 0);
        var otp = (value % 900000) + 100000; // Ensures 6-digit number
        return otp.ToString();
    }

    private static string GenerateEmailToken()
    {
        // Generate a secure random token for email verification
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
