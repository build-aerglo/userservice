using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;
using System.Text.Json;

namespace UserService.Infrastructure.Tests.Repositories;

[TestFixture]
public class EndUserProfileRepositoryTests
{
    private const string InfrastructureTestPrefix = "__INFRA_TEST__";
    private static string Unique() => Guid.NewGuid().ToString("N")[..6];

    private EndUserProfileRepository _repository = null!;
    private IConfiguration _configuration = null!;
    private string _connectionString = string.Empty;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

        _configuration = builder.Build();
        _connectionString = _configuration.GetConnectionString("PostgresConnection")
                            ?? throw new InvalidOperationException("Missing connection string");

        _repository = new EndUserProfileRepository(_configuration);
    }

    [SetUp]
    public async Task Setup()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var prefix = InfrastructureTestPrefix + "%";

        await conn.ExecuteAsync(@"
            DELETE FROM point_transactions
            WHERE user_id IN (
                SELECT id FROM users WHERE username LIKE @prefix OR email LIKE @prefix
            );", new { prefix });

        await conn.ExecuteAsync(@"
            DELETE FROM user_points
            WHERE user_id IN (
                SELECT id FROM users WHERE username LIKE @prefix OR email LIKE @prefix
            );", new { prefix });

        await conn.ExecuteAsync(@"
            DELETE FROM user_badges
            WHERE user_id IN (
                SELECT id FROM users WHERE username LIKE @prefix OR email LIKE @prefix
            );", new { prefix });

        await conn.ExecuteAsync(@"
            DELETE FROM review
            WHERE reviewer_id IN (
                SELECT id FROM users WHERE username LIKE @prefix OR email LIKE @prefix
            )
            OR email LIKE @prefix;", new { prefix });

        await conn.ExecuteAsync(@"
            DELETE FROM end_user
            WHERE user_id IN (
                SELECT id FROM users WHERE username LIKE @prefix OR email LIKE @prefix
            );", new { prefix });

        await conn.ExecuteAsync(@"
            DELETE FROM business WHERE name LIKE @prefix;", new { prefix });

        await conn.ExecuteAsync(@"
            DELETE FROM users
            WHERE username LIKE @prefix OR email LIKE @prefix;", new { prefix });
    }

    private async Task<Guid> CreateParentUserAsync(Guid? userId = null)
    {
        var id = userId ?? Guid.NewGuid();
        var unique = Unique();

        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(@"
            INSERT INTO users (id, email, username, user_type, created_at)
            VALUES (@Id, @Email, @Username, @UserType, @CreatedAt)
            ON CONFLICT (id) DO NOTHING",
            new
            {
                Id = id,
                Email = $"{InfrastructureTestPrefix}_{unique}@example.com",
                Username = $"{InfrastructureTestPrefix}_user_{unique}",
                UserType = "end_user",
                CreatedAt = DateTime.UtcNow
            });

        return id;
    }

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnProfile_WhenExists()
    {
        var userId = await CreateParentUserAsync();

        var profile = new EndUserProfile
        {
            UserId = userId,
            SocialMedia = JsonSerializer.Serialize(
                new Dictionary<string, string>
                {
                    { "twitter", "@testuser" },
                    { "instagram", "@test_insta" }
                }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
    public async Task AddAsync_ShouldInsertEndUserProfile_AndGetById_ShouldReturnProfile()
    {
        var profileId = Guid.NewGuid();
        var userId = await CreateParentUserAsync();

        var profile = new EndUserProfile
        {
            Id = profileId,
            UserId = userId,
            SocialMedia = JsonSerializer.Serialize(
                new Dictionary<string, string>
                {
                    { "twitter", "@testuser" }
                }),
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
        var userId = await CreateParentUserAsync();

        var profile = new EndUserProfile
        {
            Id = profileId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(profile);
        await _repository.DeleteAsync(profileId);

        var fetched = await _repository.GetByIdAsync(profileId);
        Assert.That(fetched, Is.Null);
    }

    [Test]
    public async Task GetUserDataAsync_WithUserId_ShouldReturnUserSummary()
    {
        var userId = await CreateParentUserAsync();
        var unique = Unique();
        var email = $"{InfrastructureTestPrefix}_{unique}@example.com";

        await using var conn = new NpgsqlConnection(_connectionString);

        var businessId = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO business (id, name, business_citytown, business_state, is_verified)
            VALUES (@Id, @Name, @City, @State, @IsVerified)",
            new
            {
                Id = businessId,
                Name = $"{InfrastructureTestPrefix}_Business_{unique}",
                City = "Test City",
                State = "TS",
                IsVerified = true
            });

        await conn.ExecuteAsync(@"
            INSERT INTO review (id, business_id, reviewer_id, email, star_rating, review_body, status, created_at)
            VALUES (@Id, @BusinessId, @ReviewerId, @Email, @StarRating, @ReviewBody, @Status, @CreatedAt)",
            new
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                ReviewerId = userId,
                Email = email,
                StarRating = 5m,
                ReviewBody = "A solid experience overall. The ambiance was pleasant and the quality was consistently good. I would definitely return in the future.",
                Status = "APPROVED",
                CreatedAt = DateTime.UtcNow
            });

        var result = await _repository.GetUserDataAsync(userId, null);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Reviews, Is.Not.Empty);
    }
    
    [Test]
public async Task GetUserDataAsync_WithEmail_ShouldReturnUserSummary()
{
    // Arrange
    var unique = Unique();
    var email = $"{InfrastructureTestPrefix}_{unique}@example.com";
    
    await using var conn = new NpgsqlConnection(_connectionString);
    
    // Insert test business
    var businessId = Guid.NewGuid();
    await conn.ExecuteAsync(@"
        INSERT INTO business (id, name, business_citytown, business_state, is_verified)
        VALUES (@Id, @Name, @City, @State, @IsVerified)
        ON CONFLICT (id) DO NOTHING",
        new { 
            Id = businessId, 
            Name = $"{InfrastructureTestPrefix}_EmailBusiness_{unique}", 
            City = "Email City", 
            State = "EC", 
            IsVerified = true 
        });

    // Insert guest review
    await conn.ExecuteAsync(@"
        INSERT INTO review (id, business_id, email, star_rating, review_body, status, is_guest_review, created_at)
        VALUES (@Id, @BusinessId, @Email, @StarRating, @ReviewBody, @Status, @IsGuest, @CreatedAt)",
        new { 
            Id = Guid.NewGuid(), 
            BusinessId = businessId, 
            Email = email,
            StarRating = 4m, 
            ReviewBody = "I had a fantastic experience visiting this business. The service was outstanding, the food was delicious, and the atmosphere was very pleasant. I would absolutely recommend it.", 
            Status = "APPROVED", 
            IsGuest = true, 
            CreatedAt = DateTime.UtcNow 
        });

    // Act
    var result = await _repository.GetUserDataAsync(null, email);

    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Email, Is.EqualTo(email));
    Assert.That(result.Reviews, Is.Not.Empty);
    
    var review = result.Reviews.First();
    Assert.That(review.StarRating, Is.EqualTo(4m));
    Assert.That(review.IsGuestReview, Is.True);
    
    Assert.That(result.Badges, Is.Empty);
    Assert.That(result.Points, Is.EqualTo(0));
}

[Test]
public async Task GetUserDataAsync_WithNoIdentifier_ShouldReturnEmptySummary()
{
    var result = await _repository.GetUserDataAsync(null, null);

    Assert.That(result, Is.Not.Null);
    Assert.That(result.UserId, Is.Null);
    Assert.That(result.Email, Is.Null);
    Assert.That(result.Reviews, Is.Empty);
    Assert.That(result.TopCities, Is.Empty);
    Assert.That(result.TopCategories, Is.Empty);
    Assert.That(result.Badges, Is.Empty);
}

[Test]
public async Task GetUserDataAsync_WithNonExistentUser_ShouldReturnEmptyData()
{
    var result = await _repository.GetUserDataAsync(Guid.NewGuid(), null);

    Assert.That(result, Is.Not.Null);
    Assert.That(result.Reviews, Is.Empty);
    Assert.That(result.TopCities, Is.Empty);
    Assert.That(result.TopCategories, Is.Empty);
    Assert.That(result.Badges, Is.Empty);
    Assert.That(result.Points, Is.EqualTo(0));
    Assert.That(result.Rank, Is.EqualTo(0));
    Assert.That(result.Streak, Is.EqualTo(0));
    Assert.That(result.LifetimePoints, Is.EqualTo(0));
}

[Test]
public async Task GetUserDataAsync_WithReviews_ShouldCalculateTopCitiesAndCategories()
{
    var unique = Unique();
    var userId = Guid.NewGuid();
    await CreateParentUserAsync(userId);
    var email = $"{InfrastructureTestPrefix}_{unique}@example.com";
    
    await using var conn = new NpgsqlConnection(_connectionString);
    
    var business1Id = Guid.NewGuid();
    await conn.ExecuteAsync(@"
        INSERT INTO business (id, name, business_citytown, business_state, is_verified)
        VALUES (@Id, @Name, @City, @State, @IsVerified)",
        new { 
            Id = business1Id, 
            Name = $"{InfrastructureTestPrefix}_Business1_{unique}", 
            City = "New York", 
            State = "NY", 
            IsVerified = true 
        });

    var business2Id = Guid.NewGuid();
    await conn.ExecuteAsync(@"
        INSERT INTO business (id, name, business_citytown, business_state, is_verified)
        VALUES (@Id, @Name, @City, @State, @IsVerified)",
        new { 
            Id = business2Id, 
            Name = $"{InfrastructureTestPrefix}_Business2_{unique}", 
            City = "Los Angeles", 
            State = "CA", 
            IsVerified = true 
        });

    var category1Id = Guid.NewGuid();
    var category2Id = Guid.NewGuid();

    await conn.ExecuteAsync(@"
        INSERT INTO category (id, name) VALUES (@Id, @Name)",
        new[] {
            new { Id = category1Id, Name = $"{InfrastructureTestPrefix}_Restaurant_{unique}" },
            new { Id = category2Id, Name = $"{InfrastructureTestPrefix}_Cafe_{unique}" }
        });

    await conn.ExecuteAsync(@"
        INSERT INTO business_category (business_id, category_id)
        VALUES (@BusinessId, @CategoryId)",
        new[] {
            new { BusinessId = business1Id, CategoryId = category1Id },
            new { BusinessId = business1Id, CategoryId = category2Id },
            new { BusinessId = business2Id, CategoryId = category2Id }
        });

    var branch1Id = Guid.NewGuid();
    var branch2Id = Guid.NewGuid();

    await conn.ExecuteAsync(@"
        INSERT INTO business_branches (id, business_id, branch_citytown, branch_state)
        VALUES (@Id, @BusinessId, @City, @State)",
        new[] {
            new { Id = branch1Id, BusinessId = business1Id, City = "New York", State = "NY" },
            new { Id = branch2Id, BusinessId = business2Id, City = "Los Angeles", State = "CA" }
        });

    var now = DateTime.UtcNow;

    for (int i = 0; i < 3; i++)
    {
        await conn.ExecuteAsync(@"
            INSERT INTO review (id, business_id, location_id, reviewer_id, email, star_rating, review_body, status, created_at)
            VALUES (@Id, @BusinessId, @LocationId, @ReviewerId, @Email, @StarRating, @ReviewBody, @Status, @CreatedAt)",
            new {
                Id = Guid.NewGuid(),
                BusinessId = business1Id,
                LocationId = branch1Id,
                ReviewerId = userId,
                Email = email,
                StarRating = 5m,
                ReviewBody = "I had a fantastic experience visiting this business. The service was outstanding, the food was delicious, and the atmosphere was very pleasant. I would absolutely recommend it.",
                Status = "APPROVED",
                CreatedAt = now.AddDays(-i)
            });
    }

    for (int i = 0; i < 2; i++)
    {
        await conn.ExecuteAsync(@"
            INSERT INTO review (id, business_id, location_id, reviewer_id, email, star_rating, review_body, status, created_at)
            VALUES (@Id, @BusinessId, @LocationId, @ReviewerId, @Email, @StarRating, @ReviewBody, @Status, @CreatedAt)",
            new {
                Id = Guid.NewGuid(),
                BusinessId = business2Id,
                LocationId = branch2Id,
                ReviewerId = userId,
                Email = email,
                StarRating = 4m,
                ReviewBody = "I had a fantastic experience. The service was excellent, the atmosphere was welcoming, and the quality exceeded expectations. I would highly recommend this business to anyone looking for great service.",
                Status = "APPROVED",
                CreatedAt = now.AddDays(-(3 + i))
            });
    }

    var result = await _repository.GetUserDataAsync(userId, null);

    Assert.That(result.Reviews.Count(), Is.EqualTo(5));

    var topCity = result.TopCities.First();
    Assert.That(topCity.City, Is.EqualTo("New York"));
    Assert.That(topCity.ReviewCount, Is.EqualTo(3));

    var topCategory = result.TopCategories.First();
    Assert.That(topCategory.ReviewCount, Is.EqualTo(5));
}

[Test]
public async Task GetUserDataAsync_WithBadges_ShouldReturnActiveBadgesOnly()
{
    // Arrange
    var userId = await CreateParentUserAsync();

    // Create end_user profile so joins work
    var profile = new EndUserProfile
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    await _repository.AddAsync(profile);

    await using var conn = new NpgsqlConnection(_connectionString);

    // Insert both active and inactive badges
    await conn.ExecuteAsync(@"
        INSERT INTO user_badges (id, user_id, badge_type, earned_at, is_active)
        VALUES 
            (@Id1, @UserId, @BadgeType1, @EarnedAt1, true),
            (@Id2, @UserId, @BadgeType2, @EarnedAt2, true),
            (@Id3, @UserId, @BadgeType3, @EarnedAt3, false)",
        new
        {
            Id1 = Guid.NewGuid(),
            UserId = userId,
            BadgeType1 = "Newbie",
            EarnedAt1 = DateTime.UtcNow.AddDays(-10),

            Id2 = Guid.NewGuid(),
            BadgeType2 = "Expert",
            EarnedAt2 = DateTime.UtcNow.AddDays(-5),

            Id3 = Guid.NewGuid(),
            BadgeType3 = "Pro",
            EarnedAt3 = DateTime.UtcNow
        });

    // Act
    var result = await _repository.GetUserDataAsync(userId, null);

    // Assert
    Assert.That(result.Badges.Count(), Is.EqualTo(2));
    Assert.That(result.Badges.Any(b => b.BadgeType == "Newbie"), Is.True);
    Assert.That(result.Badges.Any(b => b.BadgeType == "Expert"), Is.True);
    Assert.That(result.Badges.Any(b => b.BadgeType == "Pro"), Is.False);
    Assert.That(result.CurrentTier, Is.Not.Null);
}

[Test]
public async Task GetUserDataAsync_WithPoints_ShouldCalculateCorrectly()
{
    // Arrange
    var userId = await CreateParentUserAsync();

    // Create end_user profile so joins work
    var profile = new EndUserProfile
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    await _repository.AddAsync(profile);

    await using var conn = new NpgsqlConnection(_connectionString);

    await conn.ExecuteAsync(@"
        INSERT INTO user_points (user_id, total_points, current_streak, updated_at)
        VALUES (@UserId, @TotalPoints, @CurrentStreak, @UpdatedAt)",
        new
        {
            UserId = userId,
            TotalPoints = 250,
            CurrentStreak = 10,
            UpdatedAt = DateTime.UtcNow
        });

    var now = DateTime.UtcNow;

    await conn.ExecuteAsync(@"
        INSERT INTO point_transactions (id, user_id, points, transaction_type, description, created_at)
        VALUES 
            (@Id1, @UserId, @Points1, @Type1, @Desc1, @CreatedAt1),
            (@Id2, @UserId, @Points2, @Type2, @Desc2, @CreatedAt2),
            (@Id3, @UserId, @Points3, @Type3, @Desc3, @CreatedAt3)",
        new
        {
            UserId = userId,

            Id1 = Guid.NewGuid(),
            Points1 = 100,
            Type1 = "EARNED",
            Desc1 = "Wrote review",
            CreatedAt1 = now.AddDays(-2),

            Id2 = Guid.NewGuid(),
            Points2 = 50,
            Type2 = "EARNED",
            Desc2 = "Received like",
            CreatedAt2 = now.AddDays(-1),

            Id3 = Guid.NewGuid(),
            Points3 = 25,
            Type3 = "REDEEMED",
            Desc3 = "Redeemed reward",
            CreatedAt3 = now
        });

    // Act
    var result = await _repository.GetUserDataAsync(userId, null);

    // Assert
    Assert.That(result.Points, Is.EqualTo(250));
    Assert.That(result.Streak, Is.EqualTo(10));
    Assert.That(result.LifetimePoints, Is.EqualTo(175));
    Assert.That(result.RecentActivity.Count(), Is.EqualTo(3));
    Assert.That(result.RecentActivity.First().TransactionType, Is.EqualTo("REDEEMED"));
    Assert.That(result.RecentActivity.First().Points, Is.EqualTo(25));
}
   
}