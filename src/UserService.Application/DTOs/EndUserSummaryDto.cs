using UserService.Application.DTOs.Badge;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;

namespace UserService.Application.DTOs;

// Create a DTO for API responses
public class EndUserSummaryDto
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public EndUserProfileDetailDto? Profile { get; set; }
    public IEnumerable<ReviewResponseDto> Reviews { get; set; } = new List<ReviewResponseDto>();
    public IEnumerable<TopCityStat> TopCities { get; set; } = new List<TopCityStat>();
    public IEnumerable<TopCategoryStat> TopCategories { get; set; } = new List<TopCategoryStat>();
    
    // Badges - use DTOs here
    public UserBadge? TierBadge { get; set; }
    public IEnumerable<UserBadge> AchievementBadges { get; set; }
    
    // Points
    public int Points { get; set; }
    public int Rank { get; set; }
    public int Streak { get; set; }
    public int LifetimePoints { get; set; }
    public IEnumerable<PointActivityDto> RecentActivity { get; set; } = new List<PointActivityDto>();
}

// public class PointActivityDto
// {
//     public int Points { get; set; }
//     public string TransactionType { get; set; } = string.Empty;
//     public string Description { get; set; } = string.Empty;
//     public DateTime CreatedAt { get; set; }
// }
//
//
// public record ReviewResponseDto(
//     Guid Id,
//     Guid BusinessId,
//     Guid? LocationId,
//     Guid? ReviewerId,
//     string? Email,
//     decimal StarRating,
//     string ReviewBody,
//     string[]? PhotoUrls,
//     bool ReviewAsAnon,
//     bool IsGuestReview,
//     DateTime CreatedAt,
//     string Status,
//     DateTime? ValidatedAt,
//     string Name,
//     string? Logo,
//     bool IsVerified,
//     string BusinessAddress
// );

// public record TopCityStat(
//     string City,
//     string? State,
//     int ReviewCount,
//     int BusinessCount,
//     decimal AverageRating
// );
//
// public record TopCategoryStat(
//     Guid CategoryId,
//     string CategoryName,
//     int ReviewCount,
//     int BusinessCount,
//     decimal AverageRating
// );
