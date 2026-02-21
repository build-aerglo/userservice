using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Application.DTOs;
using UserService.Application.DTOs.Badge;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class EndUserProfileRepository : IEndUserProfileRepository
{
    private readonly string _connectionString;

    public EndUserProfileRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection") 
                            ?? throw new InvalidOperationException("PostgresConnection string not found in configuration");
        
        // Ensures snake_case ↔ PascalCase mapping
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task AddAsync(EndUserProfile profile)
    {
        const string sql = @"
            INSERT INTO end_user (id, user_id, social_media, created_at, updated_at)
            VALUES (@Id, @UserId, @SocialMedia, @CreatedAt, @UpdatedAt)
            ON CONFLICT (user_id) DO NOTHING;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, profile);
    }

    public async Task<EndUserProfile?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM end_user WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<EndUserProfile>(sql, new { Id = id });
    }

    public async Task<EndUserProfile?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM end_user WHERE user_id = @UserId;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<EndUserProfile>(sql, new { UserId = userId });
    }

    public async Task UpdateAsync(EndUserProfile profile)
    {
        const string sql = @"
            UPDATE end_user
            SET social_media = @SocialMedia,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, profile);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM end_user WHERE id = @Id;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
    

public async Task<EndUserSummary> GetUserDataAsync(Guid? userId, string? email, int page = 1, int pageSize = 5)
{
    // Validate: need at least one identifier
    if (!userId.HasValue && string.IsNullOrWhiteSpace(email))
    {
        return new EndUserSummary(); // Return empty object if no identifier provided
    }

    var result = new EndUserSummary
    {
        UserId = userId,
        Email = email
    };

    using var connection = CreateConnection();
    await connection.OpenAsync();

    // Get user profile if userId is available
   

    // Get user reviews with business and branch information
    const string countSql = @"
        SELECT COUNT(*)
        FROM review r
        WHERE (r.reviewer_id = @ReviewerId OR r.email = @Email);";

    var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { ReviewerId = userId, Email = email });

    // Paginated reviews query — add LIMIT + OFFSET
    const string reviewsSql = @"
        SELECT 
           r.id, r.business_id, r.location_id, r.reviewer_id, r.email, r.star_rating,
            r.review_body, r.photo_urls, r.review_as_anon, r.is_guest_review, r.created_at, r.updated_at,
            r.status, r.ip_address, r.device_id, r.geolocation, r.user_agent,
            r.validation_result, r.validated_at,
            b.name as businessname,
            b.logo as businesslogo,
            b.is_verified as businessisverified,
            b.business_citytown as business_citytown,
            b.business_state as business_state,
            b.business_address as business_address,
            bb.branch_citytown as branch_citytown,
            bb.branch_state as branch_state
        FROM review r
        INNER JOIN business b ON r.business_id = b.id
        LEFT JOIN business_branches bb ON r.location_id = bb.id
        WHERE (r.reviewer_id = @ReviewerId OR r.email = @Email)
        ORDER BY r.created_at DESC
        LIMIT @PageSize OFFSET @Offset;";

    var reviewRecords = await connection.QueryAsync<dynamic>(reviewsSql, new
    {
        ReviewerId = userId,
        Email = email,
        PageSize = pageSize,
        Offset = (page - 1) * pageSize
    });

    result.TotalReviewCount = totalCount;
    result.Reviews = reviewRecords.Select(r => 
    {
        // Build business address from available data
        string business_address;
        
        if (r.location_id != null)
        {
            // Try branch address first
            var branchCity = r.branch_citytown as string;
            var branchState = r.branch_state as string;
            
            if (!string.IsNullOrWhiteSpace(branchCity) || !string.IsNullOrWhiteSpace(branchState))
            {
                business_address = string.Join(", ", new[] { branchCity, branchState }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            else
            {
                // Fallback to business address
                var businessCity = r.business_citytown as string;
                var businessState = r.business_state as string;
                var businessAddr = r.BusinessAddress as string;
                
                if (!string.IsNullOrWhiteSpace(businessAddr))
                {
                    business_address = businessAddr;
                }
                else
                {
                    business_address = string.Join(", ", new[] { businessCity, businessState }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                    
                    if (string.IsNullOrWhiteSpace(business_address))
                    {
                        business_address = "Address not available";
                    }
                }
            }
        }
        else
        {
            // No location_id, use business address
            var businessCity = r.business_citytown as string;
            var businessState = r.business_state as string;
            var businessAddr = r.BusinessAddress as string;
            
            if (!string.IsNullOrWhiteSpace(businessAddr))
            {
                business_address = businessAddr;
            }
            else
            {
                business_address = string.Join(", ", new[] { businessCity, businessState }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
                
                if (string.IsNullOrWhiteSpace(business_address))
                {
                    business_address = "Address not available";
                }
            }
        }

        return new ReviewResponseDto(
            Id: r.id,
            BusinessId: r.business_id,
            LocationId: r.location_id,
            ReviewerId: r.reviewer_id,
            Email: r.email,
            StarRating: r.star_rating,
            ReviewBody: r.review_body,
            PhotoUrls: r.photo_urls != null ? ((string)r.photo_urls).Split(',', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>(),
            ReviewAsAnon: r.review_as_anon,
            IsGuestReview: r.is_guest_review,
            CreatedAt: r.created_at,
            Status: r.status,
            ValidatedAt: r.validated_at,
            Name: r.businessname ?? "Unknown Business",
            Logo: r.businesslogo,
            IsVerified: r.businessisverified ?? false,
            BusinessAddress: business_address
        );
    }).ToList();

    // Get top cities (if there are reviews)
    if (totalCount > 0)
    {
        const string topCitiesSql = @"
            WITH user_reviews AS (
                SELECT 
                    r.id AS review_id,
                    r.business_id,
                    r.star_rating,
                    r.location_id
                FROM review r
                WHERE (r.reviewer_id = @ReviewerId OR r.email = @Email)
                  AND r.status = 'APPROVED'
                  AND r.location_id IS NOT NULL
            ),
            city_reviews AS (
                SELECT 
                    COALESCE(
                        NULLIF(TRIM(bb.branch_citytown), ''),
                        NULLIF(TRIM(b.business_citytown), ''),
                        'Unknown City'
                    ) AS city,
                    COALESCE(
                        NULLIF(TRIM(bb.branch_state), ''),
                        NULLIF(TRIM(b.business_state), ''),
                        'Unknown'
                    ) AS state,
                    ur.review_id,
                    ur.business_id,
                    ur.star_rating
                FROM user_reviews ur
                LEFT JOIN business_branches bb ON ur.location_id = bb.id
                LEFT JOIN business b ON ur.business_id = b.id
            ),
            city_stats AS (
                SELECT 
                    cr.city,
                    cr.state,
                    COUNT(DISTINCT cr.review_id)::int AS review_count,
                    COUNT(DISTINCT cr.business_id)::int AS business_count,
                    ROUND(AVG(cr.star_rating)::numeric, 2) AS avg_rating
                FROM city_reviews cr
                GROUP BY cr.city, cr.state
            )
            SELECT 
                cs.city AS City,
                cs.state AS State,
                cs.review_count AS ReviewCount,
                cs.business_count AS BusinessCount,
                cs.avg_rating AS AverageRating
            FROM city_stats cs
            ORDER BY cs.review_count DESC, cs.avg_rating DESC
            LIMIT 3;";

        result.TopCities = (await connection.QueryAsync<TopCityStat>(topCitiesSql, new 
        { 
            ReviewerId = userId, 
            Email = email 
        })).ToList();

        // Get top categories
        const string topCategoriesSql = @"
            WITH user_reviews AS (
                SELECT 
                    r.id as review_id,
                    r.business_id,
                    r.star_rating
                FROM review r
                WHERE (r.reviewer_id = @ReviewerId OR r.email = @Email)
                  AND r.status = 'APPROVED'
            ),
            category_reviews AS (
                SELECT 
                    c.id as category_id,
                    c.name as category_name,
                    ur.review_id,
                    ur.business_id,
                    ur.star_rating
                FROM user_reviews ur
                JOIN business b ON ur.business_id = b.id
                JOIN business_category bc ON b.id = bc.business_id
                JOIN category c ON bc.category_id = c.id
            ),
            category_stats AS (
                SELECT 
                    cr.category_id,
                    cr.category_name,
                    COUNT(DISTINCT cr.review_id)::int as review_count,
                    COUNT(DISTINCT cr.business_id)::int as business_count,
                    ROUND(AVG(cr.star_rating), 2) as avg_rating
                FROM category_reviews cr
                GROUP BY cr.category_id, cr.category_name
            )
            SELECT 
                cs.category_id AS CategoryId,
                cs.category_name AS CategoryName,
                cs.review_count AS ReviewCount,
                cs.business_count AS BusinessCount,
                cs.avg_rating AS AverageRating
            FROM category_stats cs
            ORDER BY cs.review_count DESC, cs.avg_rating DESC
            LIMIT 3;";

        result.TopCategories = (await connection.QueryAsync<TopCategoryStat>(topCategoriesSql, new 
        { 
            ReviewerId = userId, 
            Email = email 
        })).ToList();
    }

    // Get user badges if userId is available
    if (userId.HasValue)
    {
        const string badgesSql = @"
            SELECT 
                id, user_id, badge_type, location, category, earned_at, is_active
            FROM user_badges 
            WHERE user_id = @UserId AND is_active = true
            ORDER BY 
                CASE 
                    WHEN badge_type IN ('Newbie', 'Expert', 'Pro') THEN 0
                    ELSE 1 
                END,
                earned_at DESC;";
        
        // CHANGE THIS: Use UserBadge (entity) instead of UserBadgeDto
        var userBadges = (await connection.QueryAsync<UserBadge>(badgesSql, new { UserId = userId })).ToList();
        result.Badges = userBadges; // Now this works
        
        // // Determine current tier
        // var tierBadges = userBadges.Where(b => 
        //     b.BadgeType == "Newbie" || 
        //     b.BadgeType == "Expert" || 
        //     b.BadgeType == "Pro").ToList();
        //
        // result.CurrentTier = tierBadges
        //     .OrderBy(b => b.BadgeType == "Pro" ? 0 : b.BadgeType == "Expert" ? 1 : 2)
        //     .FirstOrDefault()?.BadgeType ?? "Newbie";
    }

    // Get user points information if userId is available
    if (userId.HasValue)
    {
        const string pointsSummarySql = @"
        WITH user_rank AS (
            SELECT 
                up.*,
                RANK() OVER (ORDER BY up.total_points DESC) as rank,
                COALESCE((
                    SELECT SUM(points) 
                    FROM point_transactions 
                    WHERE user_id = up.user_id
                ), 0) as lifetime_points
            FROM user_points up
            WHERE up.user_id = @UserId
        )
        SELECT 
            COALESCE(total_points, 0) as points,
            COALESCE(current_streak, 0) as streak,
            COALESCE(rank, 0) as rank,
            COALESCE(lifetime_points, 0) as lifetimepoints
        FROM user_rank;";
    
        var pointsSummary = await connection.QueryFirstOrDefaultAsync<dynamic>(pointsSummarySql, new { UserId = userId });
    
        if (pointsSummary != null)
        {
            // Use Convert.ToInt32 to handle potential nulls safely
            result.Points = Convert.ToInt32(pointsSummary.points);
            result.Rank = Convert.ToInt32(pointsSummary.rank);
            result.Streak = Convert.ToInt32(pointsSummary.streak);
            result.LifetimePoints = Convert.ToInt32(pointsSummary.lifetimepoints);
        }
        else
        {
            // Set defaults if no points record exists
            result.Points = 0;
            result.Rank = 0;
            result.Streak = 0;
            result.LifetimePoints = 0;
        }

        const string recentTransactionsSql = @"
        SELECT 
            COALESCE(points, 0) as points,
            COALESCE(transaction_type, '') as TransactionType,
            COALESCE(description, '') as Description,
            created_at as CreatedAt
        FROM point_transactions 
        WHERE user_id = @UserId 
        ORDER BY created_at DESC 
        LIMIT 5;";
    
        result.RecentActivity = (await connection.QueryAsync<PointActivityDto>(recentTransactionsSql, new { UserId = userId })).ToList();
    }

    return result;
}
   
}

