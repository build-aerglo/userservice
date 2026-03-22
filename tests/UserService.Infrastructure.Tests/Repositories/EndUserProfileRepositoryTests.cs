using Dapper;
using Microsoft.Data.Sqlite;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;
using System.Data;
using System.Text.Json;

namespace UserService.Infrastructure.Tests.Repositories;

[TestFixture]
public class EndUserProfileRepositoryTests
{
    private SqliteConnection _connection = null!;
    private EndUserProfileRepository _repository = null!;

    public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override void SetValue(IDbDataParameter parameter, Guid value)
            => parameter.Value = value.ToString();

        public override Guid Parse(object value)
            => Guid.Parse((string)value);
    }

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        SqlMapper.AddTypeHandler(new GuidTypeHandler());

        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        await _connection.ExecuteAsync("PRAGMA foreign_keys = ON;");

        await CreateSchemaAsync();

        _repository = new EndUserProfileRepository(_connection);
    }

    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        await _connection.CloseAsync();
        _connection.Dispose();
    }

    [TearDown]
    public async Task TearDown()
    {
        // Order follows the FK dependency graph — children before parents:
        // review references business, business_branches, and users,
        // so it must be deleted before any of those three tables.
        await _connection.ExecuteAsync("DELETE FROM business_reply;");   // NEW — before review
        await _connection.ExecuteAsync("DELETE FROM point_transactions;");
        await _connection.ExecuteAsync("DELETE FROM user_points;");
        await _connection.ExecuteAsync("DELETE FROM user_badges;");
        await _connection.ExecuteAsync("DELETE FROM review;");
        await _connection.ExecuteAsync("DELETE FROM business_category;");
        await _connection.ExecuteAsync("DELETE FROM business_branches;");
        await _connection.ExecuteAsync("DELETE FROM end_user;");
        await _connection.ExecuteAsync("DELETE FROM business;");
        await _connection.ExecuteAsync("DELETE FROM category;");
        await _connection.ExecuteAsync("DELETE FROM users;");
    }

    private async Task CreateSchemaAsync()
    {
        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS users (
                id         TEXT PRIMARY KEY,
                email      TEXT NOT NULL,
                username   TEXT NOT NULL,
                user_type  TEXT NOT NULL,
                created_at TEXT NOT NULL
            );");

        // FIX: Added UNIQUE to user_id so that ON CONFLICT (user_id) in AddAsync
        // has a matching constraint to act on. Without UNIQUE, SQLite throws:
        // 'ON CONFLICT clause does not match any PRIMARY KEY or UNIQUE constraint'.
        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS end_user (
                id           TEXT PRIMARY KEY,
                user_id      TEXT NOT NULL UNIQUE REFERENCES users(id),
                social_media TEXT,
                created_at   TEXT NOT NULL,
                updated_at   TEXT NOT NULL
            );");

        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS business (
                id                TEXT PRIMARY KEY,
                name              TEXT NOT NULL,
                logo              TEXT,
                business_citytown TEXT,
                business_state    TEXT,
                business_address  TEXT,
                is_verified       INTEGER DEFAULT 0
            );");

        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS business_branches (
                id              TEXT PRIMARY KEY,
                business_id     TEXT NOT NULL REFERENCES business(id),
                branch_citytown TEXT,
                branch_state    TEXT
            );");

        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS category (
                id   TEXT PRIMARY KEY,
                name TEXT NOT NULL
            );");

        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS business_category (
                business_id TEXT NOT NULL REFERENCES business(id),
                category_id TEXT NOT NULL REFERENCES category(id),
                PRIMARY KEY (business_id, category_id)
            );");

        await _connection.ExecuteAsync(@"
    CREATE TABLE IF NOT EXISTS review (
        id                TEXT PRIMARY KEY,
        business_id       TEXT NOT NULL REFERENCES business(id),
        location_id       TEXT REFERENCES business_branches(id),
        reviewer_id       TEXT REFERENCES users(id),
        email             TEXT,
        star_rating       REAL,
        review_body       TEXT,
        photo_urls        TEXT,
        review_as_anon    INTEGER DEFAULT 0,
        is_guest_review   INTEGER DEFAULT 0,
        status            TEXT,
        ip_address        TEXT,
        device_id         TEXT,
        geolocation       TEXT,
        user_agent        TEXT,
        validation_result TEXT,
        validated_at      TEXT,
        created_at        TEXT NOT NULL,
        updated_at        TEXT,
        helpful_count     INTEGER DEFAULT 0
    );");

        await _connection.ExecuteAsync(@"
    CREATE TABLE IF NOT EXISTS business_reply (
        id         TEXT PRIMARY KEY,
        review_id  TEXT NOT NULL REFERENCES review(id),
        reply_body TEXT NOT NULL,
        created_at TEXT NOT NULL
    );");

        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS user_badges (
                id         TEXT PRIMARY KEY,
                user_id    TEXT NOT NULL REFERENCES users(id),
                badge_type TEXT NOT NULL,
                location   TEXT,
                category   TEXT,
                earned_at  TEXT NOT NULL,
                is_active  INTEGER DEFAULT 1
            );");

        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS user_points (
                user_id         TEXT PRIMARY KEY REFERENCES users(id),
                total_points    REAL DEFAULT 0,
                current_streak  INTEGER DEFAULT 0,
                longest_streak  INTEGER DEFAULT 0,
                last_login_date TEXT,
                updated_at      TEXT NOT NULL
            );");

        await _connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS point_transactions (
                id               TEXT PRIMARY KEY,
                user_id          TEXT NOT NULL REFERENCES users(id),
                points           REAL NOT NULL,
                transaction_type TEXT NOT NULL,
                description      TEXT,
                reference_id     TEXT,
                reference_type   TEXT,
                created_at       TEXT NOT NULL
            );");
    }

    // ========================================================================
    // HELPERS
    // ========================================================================

    private async Task<Guid> CreateUserAsync(Guid? userId = null)
    {
        var id = userId ?? Guid.NewGuid();
        await _connection.ExecuteAsync(@"
            INSERT INTO users (id, email, username, user_type, created_at)
            VALUES (@Id, @Email, @Username, @UserType, @CreatedAt)",
            new
            {
                Id        = id.ToString(),
                Email     = $"test_{id:N}@example.com",
                Username  = $"user_{id:N}",
                UserType  = "end_user",
                CreatedAt = DateTime.UtcNow.ToString("o")
            });
        return id;
    }

    private async Task<Guid> CreateBusinessAsync(string city = "Test City", string state = "TS")
    {
        var id = Guid.NewGuid();
        await _connection.ExecuteAsync(@"
            INSERT INTO business (id, name, business_citytown, business_state, is_verified)
            VALUES (@Id, @Name, @City, @State, @IsVerified)",
            new { Id = id.ToString(), Name = $"Business_{id:N}", City = city, State = state, IsVerified = 1 });
        return id;
    }

    private async Task<Guid> CreateBranchAsync(Guid businessId, string city, string state)
    {
        var id = Guid.NewGuid();
        await _connection.ExecuteAsync(@"
            INSERT INTO business_branches (id, business_id, branch_citytown, branch_state)
            VALUES (@Id, @BusinessId, @City, @State)",
            new { Id = id.ToString(), BusinessId = businessId.ToString(), City = city, State = state });
        return id;
    }

    private async Task CreateReviewAsync(Guid businessId, Guid userId, string status = "APPROVED",
        decimal starRating = 5m, Guid? locationId = null)
    {
        await _connection.ExecuteAsync(@"
            INSERT INTO review (id, business_id, location_id, reviewer_id, email, star_rating, review_body, status, created_at)
            VALUES (@Id, @BusinessId, @LocationId, @ReviewerId, @Email, @StarRating, @ReviewBody, @Status, @CreatedAt)",
            new
            {
                Id         = Guid.NewGuid().ToString(),
                BusinessId = businessId.ToString(),
                LocationId = locationId?.ToString(),
                ReviewerId = userId.ToString(),
                Email      = $"test_{userId:N}@example.com",
                StarRating = (double)starRating,
                ReviewBody = "Test review body with enough content to be meaningful.",
                Status     = status,
                CreatedAt  = DateTime.UtcNow.ToString("o")
            });
    }

    // ========================================================================
    // TESTS
    // ========================================================================

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnProfile_WhenExists()
    {
        var userId = await CreateUserAsync();
        var profile = new EndUserProfile
        {
            Id          = Guid.NewGuid(),
            UserId      = userId,
            SocialMedia = JsonSerializer.Serialize(new Dictionary<string, string> { { "twitter", "@test" } }),
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow
        };

        await _repository.AddAsync(profile);
        var fetched = await _repository.GetByUserIdAsync(userId);

        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.UserId, Is.EqualTo(userId));
    }

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnNull_WhenNotExists()
    {
        var fetched = await _repository.GetByUserIdAsync(Guid.NewGuid());
        Assert.That(fetched, Is.Null);
    }

    [Test]
    public async Task AddAsync_ShouldInsertProfile_AndGetById_ShouldReturnIt()
    {
        var profileId = Guid.NewGuid();
        var userId    = await CreateUserAsync();

        var profile = new EndUserProfile
        {
            Id        = profileId,
            UserId    = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(profile);
        var fetched = await _repository.GetByIdAsync(profileId);

        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Id, Is.EqualTo(profileId));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveProfile()
    {
        var profileId = Guid.NewGuid();
        var userId    = await CreateUserAsync();

        var profile = new EndUserProfile { Id = profileId, UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await _repository.AddAsync(profile);
        await _repository.DeleteAsync(profileId);

        var fetched = await _repository.GetByIdAsync(profileId);
        Assert.That(fetched, Is.Null);
    }

    [Test]
    public async Task GetUserDataAsync_WithUserId_ShouldReturnReviews()
    {
        var userId     = await CreateUserAsync();
        var businessId = await CreateBusinessAsync();
        await CreateReviewAsync(businessId, userId);

        var result = await _repository.GetUserDataAsync(userId, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Reviews, Is.Not.Empty);
    }

    [Test]
    public async Task GetUserDataAsync_WithNoIdentifier_ShouldReturnEmptySummary()
    {
        var result = await _repository.GetUserDataAsync(null, null);

        Assert.That(result.UserId, Is.Null);
        Assert.That(result.Reviews, Is.Empty);
        Assert.That(result.TopCities, Is.Empty);
        Assert.That(result.Badges, Is.Empty);
    }

    [Test]
    public async Task GetUserDataAsync_WithNonExistentUser_ShouldReturnEmptyData()
    {
        var result = await _repository.GetUserDataAsync(Guid.NewGuid(), null);

        Assert.That(result.Reviews, Is.Empty);
        Assert.That(result.Points, Is.EqualTo(0));
        Assert.That(result.Rank, Is.EqualTo(0));
        Assert.That(result.Streak, Is.EqualTo(0));
    }

    [Test]
    public async Task GetUserDataAsync_WithBadges_ShouldReturnActiveBadgesOnly()
    {
        var userId = await CreateUserAsync();

        await _connection.ExecuteAsync(@"
            INSERT INTO user_badges (id, user_id, badge_type, earned_at, is_active)
            VALUES 
                (@Id1, @UserId, 'Newbie', @Now, 1),
                (@Id2, @UserId, 'Expert', @Now, 1),
                (@Id3, @UserId, 'Pro',    @Now, 0)",
            new
            {
                Id1    = Guid.NewGuid().ToString(),
                Id2    = Guid.NewGuid().ToString(),
                Id3    = Guid.NewGuid().ToString(),
                UserId = userId.ToString(),
                Now    = DateTime.UtcNow.ToString("o")
            });

        var result = await _repository.GetUserDataAsync(userId, null);

        Assert.That(result.Badges.Count(), Is.EqualTo(2));
        Assert.That(result.Badges.Any(b => b.BadgeType == "Pro"), Is.False);
    }

    [Test]
    public async Task GetUserDataAsync_WithPoints_ShouldReturnCorrectValues()
    {
        var userId = await CreateUserAsync();

        await _connection.ExecuteAsync(@"
            INSERT INTO user_points (user_id, total_points, current_streak, updated_at)
            VALUES (@UserId, 250, 10, @Now)",
            new { UserId = userId.ToString(), Now = DateTime.UtcNow.ToString("o") });

        await _connection.ExecuteAsync(@"
            INSERT INTO point_transactions (id, user_id, points, transaction_type, description, created_at)
            VALUES 
                (@Id1, @UserId, 100, 'EARNED',   'Wrote review',    @T1),
                (@Id2, @UserId,  50, 'EARNED',   'Received like',   @T2),
                (@Id3, @UserId,  25, 'REDEEMED', 'Redeemed reward', @T3)",
            new
            {
                Id1    = Guid.NewGuid().ToString(),
                Id2    = Guid.NewGuid().ToString(),
                Id3    = Guid.NewGuid().ToString(),
                UserId = userId.ToString(),
                T1     = DateTime.UtcNow.AddDays(-2).ToString("o"),
                T2     = DateTime.UtcNow.AddDays(-1).ToString("o"),
                T3     = DateTime.UtcNow.ToString("o")
            });

        var result = await _repository.GetUserDataAsync(userId, null);

        Assert.That(result.Points,        Is.EqualTo(250));
        Assert.That(result.Streak,        Is.EqualTo(10));
        Assert.That(result.LifetimePoints, Is.EqualTo(175));  // 100 + 50 + 25
        Assert.That(result.RecentActivity.Count(), Is.EqualTo(3));
    }

    [Test]
    public async Task GetUserDataAsync_WithReviews_ShouldCalculateTopCitiesAndCategories()
    {
        var userId   = await CreateUserAsync();
        var biz1     = await CreateBusinessAsync("New York", "NY");
        var biz2     = await CreateBusinessAsync("Los Angeles", "CA");
        var branch1  = await CreateBranchAsync(biz1, "New York", "NY");
        var branch2  = await CreateBranchAsync(biz2, "Los Angeles", "CA");

        var cat1 = Guid.NewGuid();
        var cat2 = Guid.NewGuid();
        await _connection.ExecuteAsync("INSERT INTO category (id, name) VALUES (@Id, @Name)",
            new[] { new { Id = cat1.ToString(), Name = "Restaurant" }, new { Id = cat2.ToString(), Name = "Cafe" } });

        await _connection.ExecuteAsync("INSERT INTO business_category (business_id, category_id) VALUES (@B, @C)",
            new[]
            {
                new { B = biz1.ToString(), C = cat1.ToString() },
                new { B = biz1.ToString(), C = cat2.ToString() },
                new { B = biz2.ToString(), C = cat2.ToString() }
            });

        for (int i = 0; i < 3; i++)
            await CreateReviewAsync(biz1, userId, locationId: branch1);
        for (int i = 0; i < 2; i++)
            await CreateReviewAsync(biz2, userId, locationId: branch2);

        var result = await _repository.GetUserDataAsync(userId, null);

        Assert.That(result.Reviews.Count(),              Is.EqualTo(5));
        Assert.That(result.TopCities.First().City,       Is.EqualTo("New York"));
        Assert.That(result.TopCategories.First().ReviewCount, Is.EqualTo(5));
    }
}