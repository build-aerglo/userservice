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

    public EndUserProfileRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")
                            ?? throw new InvalidOperationException("PostgresConnection string not found in configuration");
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public EndUserProfileRepository(IDbConnection connection)
    {
        _testConnection = connection;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private bool IsTestMode => _testConnection != null;

    // SQLite stores UUIDs as TEXT and requires string params for equality checks.
    // PostgreSQL stores UUIDs as the uuid type and requires Guid params — passing
    // a string causes: "operator does not exist: uuid = text".
    // This helper returns the correct representation for whichever DB is active.
    private object GuidParam(Guid id) => IsTestMode ? (object)id.ToString() : id;

    private IDbConnection GetConnection() =>
        _testConnection ?? new NpgsqlConnection(_connectionString);

    private static async Task EnsureOpenAsync(IDbConnection connection)
    {
        if (connection.State != ConnectionState.Open)
            await ((System.Data.Common.DbConnection)connection).OpenAsync();
    }

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
            await conn.ExecuteAsync(sql, new
            {
                Id          = GuidParam(profile.Id),
                UserId      = GuidParam(profile.UserId),
                profile.SocialMedia,
                profile.CreatedAt,
                profile.UpdatedAt
            });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task<EndUserProfile?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM end_user WHERE id = @Id;";
        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            return await conn.QueryFirstOrDefaultAsync<EndUserProfile>(sql, new { Id = GuidParam(id) });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task<EndUserProfile?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM end_user WHERE user_id = @UserId;";
        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            return await conn.QueryFirstOrDefaultAsync<EndUserProfile>(sql, new { UserId = GuidParam(userId) });
        }
        finally { await DisposeIfOwnedAsync(conn); }
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
            await conn.ExecuteAsync(sql, new
            {
                Id = GuidParam(profile.Id),
                profile.SocialMedia,
                profile.UpdatedAt
            });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM end_user WHERE id = @Id;";
        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            await conn.ExecuteAsync(sql, new { Id = GuidParam(id) });
        }
        finally { await DisposeIfOwnedAsync(conn); }
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
            Email  = email
        };

        // Resolve once — GuidParam applied here so all SQL params below use it
        var reviewerIdParam = userId.HasValue ? GuidParam(userId.Value) : (object?)null;

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
                new { ReviewerId = reviewerIdParam, Email = email });

            const string reviewsSql = @"
    SELECT 
        r.id, r.business_id, r.location_id, r.reviewer_id, r.email, r.star_rating,
        r.review_body, r.photo_urls, r.review_as_anon, r.is_guest_review, r.created_at, r.updated_at,
        r.status, r.ip_address, r.device_id, r.geolocation, r.user_agent,
        r.validation_result, r.validated_at,
        r.helpful_count,
        br.reply_body AS business_reply,
        b.name        AS businessname,
        b.logo        AS businesslogo,
        b.is_verified AS businessisverified,
        b.business_citytown,
        b.business_state,
        b.business_address,
        bb.branch_citytown,
        bb.branch_state
    FROM review r
    INNER JOIN business b              ON r.business_id = b.id
    LEFT  JOIN business_branches bb    ON r.location_id  = bb.id
    LEFT  JOIN business_reply br       ON br.review_id   = r.id
    WHERE (r.reviewer_id = @ReviewerId OR r.email = @Email)
    ORDER BY r.created_at DESC
    LIMIT @PageSize OFFSET @Offset;";

            var reviewRecords = await conn.QueryAsync<dynamic>(reviewsSql, new
            {
                ReviewerId = reviewerIdParam,
                Email      = email,
                PageSize   = pageSize,
                Offset     = (page - 1) * pageSize
            });

            result.TotalReviewCount = totalCount;
            result.Reviews = reviewRecords.Select(r =>
            {
                // Postgres returns uuid columns as Guid, bool columns as bool, timestamps
                // as DateTime. SQLite returns everything as string/long/double. The helpers
                // below handle both so this code works against either database.
                var id         = GuidFromDynamic(r.id);
                var businessId = GuidFromDynamic(r.business_id);
                Guid? locationId = NullableGuidFromDynamic(r.location_id);
                Guid? reviewerId = NullableGuidFromDynamic(r.reviewer_id);

                var reviewAsAnon  = BoolFromDynamic(r.review_as_anon);
                var isGuestReview = BoolFromDynamic(r.is_guest_review);
                var isVerified    = BoolFromDynamic(r.businessisverified);
                var starRating    = DecimalFromDynamic(r.star_rating);

                var createdAt   = DateTimeFromDynamic(r.created_at);
                DateTime? validatedAt = r.validated_at != null
                    ? DateTimeFromDynamic(r.validated_at)
                    : null;

                var emailVal     = r.email as string;
                var reviewBody   = r.review_body as string;
                var status       = r.status as string;
                var businessName = (r.businessname as string) ?? "Unknown Business";
                var businessLogo = r.businesslogo as string;

                string[] photoUrls = r.photo_urls switch
                {
                    string s   => s.Split(',', StringSplitOptions.RemoveEmptyEntries),
                    string[] a => a,
                    _          => Array.Empty<string>()
                };

                string businessAddress;
                if (locationId != null)
                {
                    var branchCity  = r.branch_citytown as string;
                    var branchState = r.branch_state as string;
                    businessAddress = (!string.IsNullOrWhiteSpace(branchCity) || !string.IsNullOrWhiteSpace(branchState))
                        ? string.Join(", ", new[] { branchCity, branchState }.Where(s => !string.IsNullOrWhiteSpace(s)))
                        : BuildBusinessAddress(r);
                }
                else
                {
                    businessAddress = BuildBusinessAddress(r);
                }

                return new ReviewResponseDto(
                    Id:              id,
                    BusinessId:      businessId,
                    LocationId:      locationId,
                    ReviewerId:      reviewerId,
                    Email:           emailVal,
                    StarRating:      starRating,
                    ReviewBody:      reviewBody,
                    PhotoUrls:       photoUrls,
                    ReviewAsAnon:    reviewAsAnon,
                    IsGuestReview:   isGuestReview,
                    CreatedAt:       createdAt,
                    Status:          status,
                    ValidatedAt:     validatedAt,
                    Name:            businessName,
                    Logo:            businessLogo,
                    IsVerified:      isVerified,
                    BusinessAddress: businessAddress,
                    HelpfulCount:    Convert.ToInt32(r.helpful_count ?? 0),   // ← was hardcoded 0
                    BusinessReply:   r.business_reply as string   
                );
            }).ToList();

            // ----------------------------------------------------------------
            // Top Cities & Categories — queried as dynamic, mapped manually.
            // Dapper cannot map CTE columns with CAST to positional record
            // constructors in SQLite (returns byte[] blobs). Dynamic + manual
            // conversion is the reliable cross-db approach.
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
                            ROUND(CAST(AVG(star_rating) AS NUMERIC), 2)     AS avg_rating
                        FROM city_reviews
                        GROUP BY city, state
                    )
                    SELECT city, state, review_count, business_count, avg_rating
                    FROM city_stats
                    ORDER BY review_count DESC, avg_rating DESC
                    LIMIT 3;";

                var cityRows = await conn.QueryAsync<dynamic>(topCitiesSql,
                    new { ReviewerId = reviewerIdParam, Email = email });

                result.TopCities = cityRows.Select(row => new TopCityStat(
                    City:          (string)row.city,
                    State:         row.state as string,
                    ReviewCount:   Convert.ToInt32(row.review_count),
                    BusinessCount: Convert.ToInt32(row.business_count),
                    AverageRating: DecimalFromDynamic(row.avg_rating)
                )).ToList();

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
                            ROUND(CAST(AVG(star_rating) AS NUMERIC), 2)     AS avg_rating
                        FROM category_reviews
                        GROUP BY category_id, category_name
                    )
                    SELECT category_id, category_name, review_count, business_count, avg_rating
                    FROM category_stats
                    ORDER BY review_count DESC, avg_rating DESC
                    LIMIT 3;";

                var categoryRows = await conn.QueryAsync<dynamic>(topCategoriesSql,
                    new { ReviewerId = reviewerIdParam, Email = email });

                result.TopCategories = categoryRows.Select(row => new TopCategoryStat(
                    CategoryId:    GuidFromDynamic(row.category_id),
                    CategoryName:  (string)row.category_name,
                    ReviewCount:   Convert.ToInt32(row.review_count),
                    BusinessCount: Convert.ToInt32(row.business_count),
                    AverageRating: DecimalFromDynamic(row.avg_rating)
                )).ToList();
            }

            // ----------------------------------------------------------------
            // Badges
            // SQLite stores booleans as INTEGER → requires `is_active = 1`
            // Postgres stores booleans as BOOLEAN → requires `is_active = true`
            // ----------------------------------------------------------------
            if (userId.HasValue)
            {
                var isActiveValue = IsTestMode ? "1" : "true";
                var badgesSql = $@"
                    SELECT id, user_id, badge_type, location, category, earned_at, is_active
                    FROM user_badges
                    WHERE user_id = @UserId AND is_active = {isActiveValue}
                    ORDER BY
                        CASE badge_type
                            WHEN 'Newbie' THEN 0
                            WHEN 'Expert' THEN 0
                            WHEN 'Pro'    THEN 0
                            ELSE 1
                        END,
                        earned_at DESC;";

                var userBadges = (await conn.QueryAsync<UserBadge>(badgesSql,
                    new { UserId = reviewerIdParam })).ToList();

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
                    new { UserId = reviewerIdParam });

                if (pointsSummary != null)
                {
                    result.Points         = Convert.ToInt32(pointsSummary.points);
                    result.Streak         = Convert.ToInt32(pointsSummary.streak);
                    result.LifetimePoints = Convert.ToInt32(pointsSummary.lifetimepoints);

                    const string rankSql = @"
                        WITH ranked AS (
                            SELECT user_id,
                                   ROW_NUMBER() OVER (ORDER BY total_points DESC) AS rank
                            FROM user_points
                            WHERE total_points > 0
                        )
                        SELECT rank FROM ranked WHERE user_id = @UserId;";

                    var rankValue = await conn.ExecuteScalarAsync<long?>(rankSql, new { UserId = reviewerIdParam });
                    result.Rank = rankValue.HasValue ? Convert.ToInt32(rankValue.Value) : 0;
                }
                else
                {
                    result.Points         = 0;
                    result.Rank           = 0;
                    result.Streak         = 0;
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
                    new { UserId = reviewerIdParam })).ToList();
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

    // Cross-database dynamic field helpers.
    //
    // Postgres (Npgsql) returns uuid columns as Guid, boolean as bool, timestamps
    // as DateTime, and numerics as double/decimal.
    // SQLite (Microsoft.Data.Sqlite) returns everything as string/long/double.
    // These helpers accept either form and always return the correct CLR type.

    private static Guid GuidFromDynamic(object? val) => val switch
    {
        Guid g   => g,
        string s => Guid.Parse(s),
        _        => throw new InvalidCastException($"Cannot convert {val?.GetType().Name ?? "null"} to Guid")
    };

    private static Guid? NullableGuidFromDynamic(object? val) => val switch
    {
        null     => null,
        Guid g   => g,
        string s => Guid.Parse(s),
        _        => throw new InvalidCastException($"Cannot convert {val.GetType().Name} to Guid?")
    };

    private static bool BoolFromDynamic(object? val) => val switch
    {
        bool b   => b,                          // Postgres boolean column
        long l   => l != 0,                     // SQLite INTEGER column
        int i    => i != 0,
        null     => false,
        _        => Convert.ToBoolean(val)      // fallback
    };

    private static decimal DecimalFromDynamic(object? val) => val switch
    {
        decimal d => d,
        double db => (decimal)db,
        float f   => (decimal)f,
        long l    => l,
        int i     => i,
        null      => 0m,
        _         => Convert.ToDecimal(val)
    };

    private static DateTime DateTimeFromDynamic(object? val) => val switch
    {
        DateTime dt => dt,                      // Postgres returns DateTime directly
        string s    => DateTime.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind),
        _           => throw new InvalidCastException($"Cannot convert {val?.GetType().Name ?? "null"} to DateTime")
    };
}