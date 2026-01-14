using System.Text;
using UserService.Application.DTOs.Referral;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class ReferralService(
    IUserReferralCodeRepository referralCodeRepository,
    IReferralRepository referralRepository,
    IUserRepository userRepository,
    IPointsService pointsService,
    IVerificationService verificationService
) : IReferralService
{
    // Required number of approved reviews to qualify the referral
    private const int RequiredApprovedReviews = 3;

    public async Task<UserReferralCodeDto?> GetUserReferralCodeAsync(Guid userId)
    {
        var referralCode = await referralCodeRepository.GetByUserIdAsync(userId);
        return referralCode is null ? null : MapToDto(referralCode);
    }

    public async Task<UserReferralCodeDto> GenerateReferralCodeAsync(GenerateReferralCodeDto dto)
    {
        // Validate user exists
        var user = await userRepository.GetByIdAsync(dto.UserId);
        if (user is null)
            throw new EndUserNotFoundException(dto.UserId);

        // Check if user already has a referral code
        var existingCode = await referralCodeRepository.GetByUserIdAsync(dto.UserId);
        if (existingCode is not null)
            throw new ReferralCodeAlreadyExistsException(dto.UserId);

        // Generate unique code based on username
        var code = await GenerateUniqueCodeAsync(user.Username);

        var referralCode = new UserReferralCode(dto.UserId, code);
        await referralCodeRepository.AddAsync(referralCode);

        var savedCode = await referralCodeRepository.GetByIdAsync(referralCode.Id);
        if (savedCode is null)
            throw new InvalidReferralCodeException(code);

        return MapToDto(savedCode);
    }

    public async Task<ApplyReferralCodeResponseDto> ApplyReferralCodeAsync(ApplyReferralCodeDto dto)
    {
        // Validate the new user exists
        var user = await userRepository.GetByIdAsync(dto.UserId);
        if (user is null)
            throw new EndUserNotFoundException(dto.UserId);

        // Check if user was already referred
        var existingReferral = await referralRepository.GetByReferredUserIdAsync(dto.UserId);
        if (existingReferral is not null)
            throw new UserAlreadyReferredException(dto.UserId);

        // Validate the referral code exists and is active
        var referralCode = await referralCodeRepository.GetByCodeAsync(dto.Code);
        if (referralCode is null)
            throw new ReferralCodeNotFoundException(dto.Code);

        if (!referralCode.IsActive)
            throw new ReferralCodeInactiveException(dto.Code);

        // Prevent self-referral
        if (referralCode.UserId == dto.UserId)
            throw new SelfReferralException();

        // Create referral record
        var referral = new Referral(
            referrerId: referralCode.UserId,
            referredUserId: dto.UserId,
            referralCode: dto.Code
        );

        await referralRepository.AddAsync(referral);

        // Increment total referrals count
        referralCode.IncrementTotalReferrals();
        await referralCodeRepository.UpdateAsync(referralCode);

        return new ApplyReferralCodeResponseDto(
            Success: true,
            Message: "Referral code applied successfully",
            ReferrerId: referralCode.UserId
        );
    }

    public async Task<ReferralListResponseDto> GetUserReferralsAsync(Guid userId)
    {
        var referrals = await referralRepository.GetByReferrerIdAsync(userId);
        var referralDtos = referrals.Select(MapToDto).ToList();

        var stats = await GetReferralStatsAsync(userId);

        return new ReferralListResponseDto(
            UserId: userId,
            Referrals: referralDtos,
            TotalCount: referralDtos.Count,
            Stats: stats
        );
    }

    public async Task<ReferralStatsDto> GetReferralStatsAsync(Guid userId)
    {
        var referralCode = await referralCodeRepository.GetByUserIdAsync(userId);
        if (referralCode is null)
        {
            return new ReferralStatsDto(
                UserId: userId,
                Code: string.Empty,
                TotalReferrals: 0,
                PendingReferrals: 0,
                SuccessfulReferrals: 0,
                TotalPointsEarned: 0
            );
        }

        var referrals = await referralRepository.GetByReferrerIdAsync(userId);
        var pendingCount = referrals.Count(r =>
            r.Status is ReferralStatuses.Registered or ReferralStatuses.Active or ReferralStatuses.Qualified);

        // Calculate total points earned from referrals
        // (Assuming 50/75 points per successful referral based on verification status)
        var isVerified = await verificationService.IsUserVerifiedAsync(userId);
        var pointsPerReferral = isVerified ? 75m : 50m;
        var totalPointsEarned = referralCode.SuccessfulReferrals * pointsPerReferral;

        return new ReferralStatsDto(
            UserId: userId,
            Code: referralCode.Code,
            TotalReferrals: referralCode.TotalReferrals,
            PendingReferrals: pendingCount,
            SuccessfulReferrals: referralCode.SuccessfulReferrals,
            TotalPointsEarned: totalPointsEarned
        );
    }

    public async Task ProcessReferralReviewAsync(ProcessReferralReviewDto dto)
    {
        if (!dto.IsApproved)
            return;

        // Check if the user was referred
        var referral = await referralRepository.GetByReferredUserIdAsync(dto.ReferredUserId);
        if (referral is null)
            return;

        // If already completed, nothing to do
        if (referral.Status == ReferralStatuses.Completed)
            return;

        // Increment approved review count
        referral.IncrementApprovedReviewCount();
        await referralRepository.UpdateAsync(referral);

        // Check if referral is now qualified (3 approved reviews)
        if (referral.Status == ReferralStatuses.Qualified && !referral.PointsAwarded)
        {
            await CompleteReferralAsync(referral.Id);
        }
    }

    public async Task CompleteReferralAsync(Guid referralId)
    {
        var referral = await referralRepository.GetByIdAsync(referralId);
        if (referral is null)
            throw new ReferralNotFoundException(referralId);

        if (referral.PointsAwarded)
            throw new ReferralAlreadyCompletedException(referralId);

        // Check if referral is qualified
        if (referral.ApprovedReviewCount < RequiredApprovedReviews)
            return;

        // Award points to referrer
        var isReferrerVerified = await verificationService.IsUserVerifiedAsync(referral.ReferrerId);
        await pointsService.AwardReferralBonusAsync(referral.ReferrerId, referralId, isReferrerVerified);

        // Mark referral as completed
        referral.MarkAsCompleted();
        await referralRepository.UpdateAsync(referral);

        // Update referral code stats
        var referralCode = await referralCodeRepository.GetByCodeAsync(referral.ReferralCode);
        if (referralCode is not null)
        {
            referralCode.IncrementSuccessfulReferrals();
            await referralCodeRepository.UpdateAsync(referralCode);
        }
    }

    public async Task ProcessQualifiedReferralsAsync()
    {
        // Get all qualified but not completed referrals
        var qualifiedReferrals = await referralRepository.GetQualifiedButNotCompletedAsync();

        foreach (var referral in qualifiedReferrals)
        {
            try
            {
                await CompleteReferralAsync(referral.Id);
            }
            catch (Exception)
            {
                // Log error but continue processing other referrals
                // In production, use proper logging
            }
        }
    }

    public async Task<IEnumerable<TopReferrerDto>> GetTopReferrersAsync(int limit = 10)
    {
        var topCodes = await referralCodeRepository.GetTopReferrersAsync(limit);
        var result = new List<TopReferrerDto>();

        int rank = 1;
        foreach (var code in topCodes)
        {
            var user = await userRepository.GetByIdAsync(code.UserId);
            var isVerified = await verificationService.IsUserVerifiedAsync(code.UserId);
            var pointsPerReferral = isVerified ? 75m : 50m;

            result.Add(new TopReferrerDto(
                Rank: rank++,
                UserId: code.UserId,
                Username: user?.Username ?? "Unknown",
                Code: code.Code,
                SuccessfulReferrals: code.SuccessfulReferrals,
                TotalPointsEarned: code.SuccessfulReferrals * pointsPerReferral
            ));
        }

        return result;
    }

    public async Task<bool> ValidateReferralCodeAsync(string code)
    {
        var referralCode = await referralCodeRepository.GetByCodeAsync(code);
        return referralCode is not null && referralCode.IsActive;
    }

    public async Task<bool> WasUserReferredAsync(Guid userId)
    {
        var referral = await referralRepository.GetByReferredUserIdAsync(userId);
        return referral is not null;
    }

    public async Task<string> GenerateUniqueCodeAsync(string username)
    {
        // Generate code based on username + year + random suffix
        var baseCode = CleanUsername(username).ToUpperInvariant();
        var year = DateTime.UtcNow.Year;

        // Try simple code first
        var code = $"{baseCode}{year}";
        if (!await referralCodeRepository.CodeExistsAsync(code))
            return code;

        // Add random suffix if needed
        var random = new Random();
        for (int i = 0; i < 10; i++)
        {
            var suffix = random.Next(10, 99);
            code = $"{baseCode}{year}{suffix}";
            if (!await referralCodeRepository.CodeExistsAsync(code))
                return code;
        }

        // Last resort: use GUID suffix
        code = $"{baseCode}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        return code;
    }

    private static string CleanUsername(string username)
    {
        // Keep only alphanumeric characters, limit to first 8 chars
        var sb = new StringBuilder();
        foreach (var c in username)
        {
            if (char.IsLetterOrDigit(c) && sb.Length < 8)
                sb.Append(c);
        }
        return sb.Length > 0 ? sb.ToString() : "USER";
    }

    private static UserReferralCodeDto MapToDto(UserReferralCode referralCode)
    {
        return new UserReferralCodeDto(
            UserId: referralCode.UserId,
            Code: referralCode.Code,
            TotalReferrals: referralCode.TotalReferrals,
            SuccessfulReferrals: referralCode.SuccessfulReferrals,
            IsActive: referralCode.IsActive,
            CreatedAt: referralCode.CreatedAt
        );
    }

    private static ReferralDto MapToDto(Referral referral)
    {
        return new ReferralDto(
            Id: referral.Id,
            ReferrerId: referral.ReferrerId,
            ReferredUserId: referral.ReferredUserId,
            ReferralCode: referral.ReferralCode,
            Status: referral.Status,
            ApprovedReviewCount: referral.ApprovedReviewCount,
            PointsAwarded: referral.PointsAwarded,
            QualifiedAt: referral.QualifiedAt,
            CompletedAt: referral.CompletedAt,
            CreatedAt: referral.CreatedAt
        );
    }
}
