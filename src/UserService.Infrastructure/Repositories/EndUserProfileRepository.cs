using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using UserService.Application.DTOs;
using UserService.Application.DTOs.Badge;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class EndUserProfileRepository : IEndUserProfileRepository
{
    private readonly string? _connectionString;
    private readonly IDbConnection? _testConnection;

    // Production constructor
    public EndUserProfileRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")
                            ?? throw new InvalidOperationException("PostgresConnection string not found in configuration");
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    // Test constructor
    public EndUserProfileRepository(IDbConnection connection)
    {
        _testConnection = connection;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private bool IsTestMode => _testConnection != null;

    // Returns connection — only caller-owned (non-test) connections should be disposed
    private IDbConnection GetConnection() =>
        _testConnection ?? new NpgsqlConnection(_connectionString);

    // Safely open only if not already open
    private static async Task EnsureOpenAsync(IDbConnection connection)
    {
        if (connection.State != ConnectionState.Open)
            await ((System.Data.Common.DbConnection)connection).OpenAsync();
    }

    // Dispose only if we own it (i.e. not the injected test connection)
    private async Task DisposeIfOwnedAsync(IDbConnection connection)
    {
        if (!IsTestMode)
            await ((IAsyncDisposable)connection).DisposeAsync();
    }

    // ========================================================================
    // CRUD
    // ========================================================================

    public async Task AddAsync(EndUserProfile profile)
    {
        const string sql = @"
            INSERT INTO end_user (id, user_id, social_media, created_at, updated_at)
            VALUES (@Id, @UserId, @SocialMedia, @CreatedAt, @UpdatedAt)
            ON CONFLICT (user_id) DO NOTHING;";

        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            await conn.ExecuteAsync(sql, profile);
        }
        finally
        {
            await DisposeIfOwnedAsync(conn);
        }
    }

    public async Task<EndUserProfile?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM end_user WHERE id = @Id;";

        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            return await conn.QueryFirstOrDefaultAsync<EndUserProfile>(sql, new { Id = id });
        }
        finally
        {
            await DisposeIfOwnedAsync(conn);
        }
    }

    public async Task<EndUserProfile?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM end_user WHERE user_id = @UserId;";

        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            return await conn.QueryFirstOrDefaultAsync<EndUserProfile>(sql, new { UserId = userId });
        }
        finally
        {
            await DisposeIfOwnedAsync(conn);
        }
    }

    public async Task UpdateAsync(EndUserProfile profile)
    {
        const string sql = @"
            UPDATE end_user
            SET social_media = @SocialMedia,
                updated_at   = @UpdatedAt
            WHERE id = @Id;";

        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            await conn.ExecuteAsync(sql, profile);
        }
        finally
        {
            await DisposeIfOwnedAsync(conn);
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM end_user WHERE id = @Id;";

        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            await conn.ExecuteAsync(sql, new { Id = id });
        }
        finally
        {
            await DisposeIfOwnedAsync(conn);
        }
    }

    // ========================================================================
    // GET USER DATA
    // ========================================================================

   public async Task<EndUserSummary> GetUserDataAsync(Guid? userId, string? email, int page = 1, int pageSize = 5)
{
    if (!userId.HasValue && string.IsNullOrWhiteSpace(email))
        return new EndUserSummary();

    var result = new EndUserSummary
    {
        UserId = userId,
        Email = email
    };

    var conn = GetConnection();
    try
    {
        await EnsureOpenAsync(conn);

        // ----------------------------------------------------------------
        // Reviews
        // ----------------------------------------------------------------
        const string countSql = @"
            SELECT COUNT(*)
            FROM review r
            WHERE (r.reviewer_id = @ReviewerId OR r.email = @Email);";

        var totalCount = await conn.ExecuteScalarAsync<int>(countSql,
            new { ReviewerId = userId, Email = email });

        // NOTE: LIMIT/OFFSET works in both Postgres and SQLite
        const string reviewsSql = @"
            SELECT 
                r.id, r.business_id, r.location_id, r.reviewer_id, r.email, r.star_rating,
                r.review_body, r.photo_urls, r.review_as_anon, r.is_guest_review, r.created_at, r.updated_at,
                r.status, r.ip_address, r.device_id, r.geolocation, r.user_agent,
                r.validation_result, r.validated_at,
                b.name        AS businessname,
                b.logo        AS businesslogo,
                b.is_verified AS businessisverified,
                b.business_citytown,
                b.business_state,
                b.business_address,
                bb.branch_citytown,
                bb.branch_state
            FROM review r
            INNER JOIN business b         ON r.business_id  = b.id
            LEFT  JOIN business_branches bb ON r.location_id = bb.id
            WHERE (r.reviewer_id = @ReviewerId OR r.email = @Email)
            ORDER BY r.created_at DESC
            LIMIT @PageSize OFFSET @Offset;";

        var reviewRecords = await conn.QueryAsync<dynamic>(reviewsSql, new
        {
            ReviewerId = userId,
            Email = email,
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        });

        result.TotalReviewCount = totalCount;
        result.Reviews = reviewRecords.Select(r =>
        {
            string businessAddress;

            if (r.location_id != null)
            {
                var branchCity  = r.branch_citytown as string;
                var branchState = r.branch_state as string;

                if (!string.IsNullOrWhiteSpace(branchCity) || !string.IsNullOrWhiteSpace(branchState))
                {
                    businessAddress = string.Join(", ",
                        new[] { branchCity, branchState }.Where(s => !string.IsNullOrWhiteSpace(s)));
                }
                else
                {
                    businessAddress = BuildBusinessAddress(r);
                }
            }
            else
            {
                businessAddress = BuildBusinessAddress(r);
            }

            return new ReviewResponseDto(
                Id: r.id,
                BusinessId: r.business_id,
                LocationId: r.location_id,
                ReviewerId: r.reviewer_id,
                Email: r.email,
                StarRating: r.star_rating,
                ReviewBody: r.review_body,
                PhotoUrls: r.photo_urls switch
                {
                    string s   => s.Split(',', StringSplitOptions.RemoveEmptyEntries),
                    string[] a => a,
                    _          => Array.Empty<string>()
                },
                ReviewAsAnon: r.review_as_anon,
                IsGuestReview: r.is_guest_review,
                CreatedAt: r.created_at,
                Status: r.status,
                ValidatedAt: r.validated_at,
                Name: r.businessname ?? "Unknown Business",
                Logo: r.businesslogo,
                IsVerified: r.businessisverified ?? false,
                BusinessAddress: businessAddress
            );
        }).ToList();

        // ----------------------------------------------------------------
        // Top Cities & Categories  
        // NOTE: Removed ::int and ::numeric Postgres casts — CAST() works in both
        // ----------------------------------------------------------------
        if (totalCount > 0)
        {
            const string topCitiesSql = @"
                WITH user_reviews AS (
                    SELECT r.id AS review_id, r.business_id, r.star_rating, r.location_id
                    FROM review r
                    WHERE (r.reviewer_id = @ReviewerId OR r.email = @Email)
                      AND r.status = 'APPROVED'
                      AND r.location_id IS NOT NULL
                ),
                city_reviews AS (
                    SELECT 
                        COALESCE(NULLIF(TRIM(bb.branch_citytown), ''), NULLIF(TRIM(b.business_citytown), ''), 'Unknown City') AS city,
                        COALESCE(NULLIF(TRIM(bb.branch_state),    ''), NULLIF(TRIM(b.business_state),    ''), 'Unknown')      AS state,
                        ur.review_id, ur.business_id, ur.star_rating
                    FROM user_reviews ur
                    LEFT JOIN business_branches bb ON ur.location_id = bb.id
                    LEFT JOIN business b           ON ur.business_id  = b.id
                ),
                city_stats AS (
                    SELECT 
                        city, state,
                        CAST(COUNT(DISTINCT review_id)   AS INTEGER) AS review_count,
                        CAST(COUNT(DISTINCT business_id) AS INTEGER) AS business_count,
                        ROUND(CAST(AVG(star_rating) AS NUMERIC), 2)  AS avg_rating
                    FROM city_reviews
                    GROUP BY city, state
                )
                SELECT city AS City, state AS State, review_count AS ReviewCount,
                       business_count AS BusinessCount, avg_rating AS AverageRating
                FROM city_stats
                ORDER BY review_count DESC, avg_rating DESC
                LIMIT 3;";

            result.TopCities = (await conn.QueryAsync<TopCityStat>(topCitiesSql,
                new { ReviewerId = userId, Email = email })).ToList();

            const string topCategoriesSql = @"
                WITH user_reviews AS (
                    SELECT r.id AS review_id, r.business_id, r.star_rating
                    FROM review r
                    WHERE (r.reviewer_id = @ReviewerId OR r.email = @Email)
                      AND r.status = 'APPROVED'
                ),
                category_reviews AS (
                    SELECT c.id AS category_id, c.name AS category_name,
                           ur.review_id, ur.business_id, ur.star_rating
                    FROM user_reviews ur
                    JOIN business          b  ON ur.business_id  = b.id
                    JOIN business_category bc ON b.id            = bc.business_id
                    JOIN category          c  ON bc.category_id  = c.id
                ),
                category_stats AS (
                    SELECT 
                        category_id, category_name,
                        CAST(COUNT(DISTINCT review_id)   AS INTEGER) AS review_count,
                        CAST(COUNT(DISTINCT business_id) AS INTEGER) AS business_count,
                        ROUND(CAST(AVG(star_rating) AS NUMERIC), 2)  AS avg_rating
                    FROM category_reviews
                    GROUP BY category_id, category_name
                )
                SELECT category_id AS CategoryId, category_name AS CategoryName,
                       review_count AS ReviewCount, business_count AS BusinessCount,
                       avg_rating AS AverageRating
                FROM category_stats
                ORDER BY review_count DESC, avg_rating DESC
                LIMIT 3;";

            result.TopCategories = (await conn.QueryAsync<TopCategoryStat>(topCategoriesSql,
                new { ReviewerId = userId, Email = email })).ToList();
        }

        // ----------------------------------------------------------------
        // Badges - FIXED: is_active = true instead of is_active = 1
        // ----------------------------------------------------------------
        if (userId.HasValue)
        {
            const string badgesSql = @"
                SELECT id, user_id, badge_type, location, category, earned_at, is_active
                FROM user_badges
                WHERE user_id = @UserId AND is_active = true
                ORDER BY
                    CASE badge_type
                        WHEN 'Newbie' THEN 0
                        WHEN 'Expert' THEN 0
                        WHEN 'Pro'    THEN 0
                        ELSE 1
                    END,
                    earned_at DESC;";

            var userBadges = (await conn.QueryAsync<UserBadge>(badgesSql,
                new { UserId = userId })).ToList();

            result.Badges = userBadges;
        }

        // ----------------------------------------------------------------
        // Points
        // ----------------------------------------------------------------
        if (userId.HasValue)
        {
            const string pointsSql = @"
                SELECT 
                    COALESCE(total_points,    0) AS points,
                    COALESCE(current_streak,  0) AS streak,
                    COALESCE(longest_streak,  0) AS longeststreak,
                    COALESCE((
                        SELECT SUM(points) FROM point_transactions WHERE user_id = @UserId
                    ), 0) AS lifetimepoints
                FROM user_points
                WHERE user_id = @UserId;";

            var pointsSummary = await conn.QueryFirstOrDefaultAsync<dynamic>(pointsSql,
                new { UserId = userId });

            if (pointsSummary != null)
            {
                result.Points        = Convert.ToInt32(pointsSummary.points);
                result.Streak        = Convert.ToInt32(pointsSummary.streak);
                result.LifetimePoints = Convert.ToInt32(pointsSummary.lifetimepoints);

                const string rankSql = @"
                    WITH ranked AS (
                        SELECT user_id, 
                               ROW_NUMBER() OVER (ORDER BY total_points DESC) as rank
                        FROM user_points
                        WHERE total_points > 0
                    )
                    SELECT rank FROM ranked WHERE user_id = @UserId;";

                result.Rank = Convert.ToInt32(
                    await conn.ExecuteScalarAsync<long>(rankSql, new { UserId = userId }));
            }
            else
            {
                result.Points        = 0;
                result.Rank          = 0;
                result.Streak        = 0;
                result.LifetimePoints = 0;
            }

            const string recentTransactionsSql = @"
                SELECT 
                    COALESCE(points,           0)  AS points,
                    COALESCE(transaction_type, '') AS TransactionType,
                    COALESCE(description,      '') AS Description,
                    created_at                     AS CreatedAt
                FROM point_transactions
                WHERE user_id = @UserId
                ORDER BY created_at DESC
                LIMIT 5;";

            result.RecentActivity = (await conn.QueryAsync<PointActivityDto>(recentTransactionsSql,
                new { UserId = userId })).ToList();
        }

        return result;
    }
    finally
    {
        await DisposeIfOwnedAsync(conn);
    }
}
    // ========================================================================
    // PRIVATE HELPERS
    // ========================================================================

    private static string BuildBusinessAddress(dynamic r)
    {
        var businessAddr  = r.business_address as string;
        var businessCity  = r.business_citytown as string;
        var businessState = r.business_state as string;

        if (!string.IsNullOrWhiteSpace(businessAddr))
            return businessAddr;

        var cityState = string.Join(", ",
            new[] { businessCity, businessState }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return string.IsNullOrWhiteSpace(cityState) ? "Address not available" : cityState;
    }
}