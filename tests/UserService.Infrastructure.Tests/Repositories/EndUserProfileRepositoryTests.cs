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

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Check if required tables exist
        var endUserTableExists = await conn.ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT FROM information_schema.tables WHERE table_name = 'end_user'
            );
        ");

        if (!endUserTableExists)
            Assert.Fail("❌ Table 'end_user' does not exist. Run migrations first.");

        // Check if other required tables exist for GetUserDataAsync
        var tablesExist = await conn.ExecuteScalarAsync<bool>(@"
            SELECT 
                EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'review') AND
                EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'business') AND
                EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'business_branches') AND
                EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'user_badges') AND
                EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'user_points') AND
                EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'point_transactions')
        ");

        if (!tablesExist)
            Assert.Warn("⚠️ Some related tables are missing. Some tests may fail.");
    }

    [SetUp]
    public async Task Setup()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        
        // Clean up all related tables (order matters due to FK constraints)
        await conn.ExecuteAsync("DELETE FROM point_transactions;");
        await conn.ExecuteAsync("DELETE FROM user_points;");
        await conn.ExecuteAsync("DELETE FROM user_badges;");
        await conn.ExecuteAsync("DELETE FROM review;");
        await conn.ExecuteAsync("DELETE FROM business_branches;");
        await conn.ExecuteAsync("DELETE FROM business_category;");
        await conn.ExecuteAsync("DELETE FROM category;");
        await conn.ExecuteAsync("DELETE FROM business;");
        await conn.ExecuteAsync("DELETE FROM end_user;");
        await conn.ExecuteAsync("DELETE FROM users;");
    }

    /// <summary>
    /// Creates a parent user record in the "users" table to satisfy the FK constraint on end_user.
    /// </summary>
    private async Task<Guid> CreateParentUserAsync(Guid? userId = null)
    {
        var id = userId ?? Guid.NewGuid();
        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(@"
            INSERT INTO users (id, email, username, user_type, created_at)
            VALUES (@Id, @Email, @Username, @UserType, @CreatedAt)
            ON CONFLICT (id) DO NOTHING",
            new { Id = id, Email = $"test-{id}@example.com", Username = $"testuser-{id:N}", UserType = "end_user", CreatedAt = DateTime.UtcNow });

        return id;
    }

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnProfile_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateParentUserAsync(userId);

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

        // Act
        var fetched = await _repository.GetByUserIdAsync(userId);

        // Assert
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.UserId, Is.EqualTo(userId));
        var socialMediaDict = JsonSerializer.Deserialize<Dictionary<string, string>>(fetched.SocialMedia!);

        Assert.That(socialMediaDict!["instagram"], Is.EqualTo("@test_insta"));
    }

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var fetched = await _repository.GetByUserIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(fetched, Is.Null);
    }
    
    [Test]
    public async Task UpdateAsync_ShouldModifyExistingProfile()
    {
        var profileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await CreateParentUserAsync(userId);

        var profile = new EndUserProfile
        {
            Id = profileId,
            UserId = userId,
            SocialMedia = JsonSerializer.Serialize(
                new Dictionary<string, string>
                {
                    { "twitter", "@old_handle" }
                }),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        await _repository.AddAsync(profile);

        var updatedProfile = new EndUserProfile
        {
            Id = profileId,
            UserId = userId,
            SocialMedia = JsonSerializer.Serialize(
                new Dictionary<string, string>
                {
                    { "twitter", "@new_handle" },
                    { "facebook", "new_profile" }
                }),
            CreatedAt = profile.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.UpdateAsync(updatedProfile);
        var fetched = await _repository.GetByIdAsync(profileId);

        Assert.That(fetched, Is.Not.Null);

        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(fetched!.SocialMedia!);

        Assert.That(dict!["twitter"], Is.EqualTo("@new_handle"));
        Assert.That(dict["facebook"], Is.EqualTo("new_profile"));
        Assert.That(fetched.CreatedAt, Is.EqualTo(profile.CreatedAt).Within(TimeSpan.FromMilliseconds(1)));
    }
    
    [Test]
    public async Task AddAsync_ShouldInsertEndUserProfile_AndGetById_ShouldReturnProfile()
    {
        var profileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await CreateParentUserAsync(userId);

        var profile = new EndUserProfile
        {
            Id = profileId,
            UserId = userId,
            SocialMedia = JsonSerializer.Serialize(
                new Dictionary<string, string>
                {
                    { "twitter", "@testuser" },
                    { "linkedin", "testuser-profile" }
                }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(profile);
        var fetched = await _repository.GetByIdAsync(profileId);

        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Id, Is.EqualTo(profileId));
        Assert.That(fetched.UserId, Is.EqualTo(userId));

        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(fetched.SocialMedia!);
        Assert.That(dict!["twitter"], Is.EqualTo("@testuser"));
    }

    [Test]
    public async Task AddAsync_WithDuplicateUserId_ShouldDoNothing()
    {
        var userId = Guid.NewGuid();
        await CreateParentUserAsync(userId);

        var profile1 = new EndUserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SocialMedia = JsonSerializer.Serialize(
                new Dictionary<string, string>
                {
                    { "twitter", "@user1" }
                }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var profile2 = new EndUserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SocialMedia = JsonSerializer.Serialize(
                new Dictionary<string, string>
                {
                    { "twitter", "@user2" }
                }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(profile1);
        await _repository.AddAsync(profile2);

        var fetched = await _repository.GetByUserIdAsync(userId);

        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(fetched!.SocialMedia!);

        Assert.That(dict!["twitter"], Is.EqualTo("@user1"));
    }
    
    [Test]
    public async Task DeleteAsync_ShouldRemoveProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await CreateParentUserAsync(userId);

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

        // Act
        await _repository.DeleteAsync(profileId);
        var fetched = await _repository.GetByIdAsync(profileId);

        // Assert
        Assert.That(fetched, Is.Null);
    }

    [Test]
    public async Task GetUserDataAsync_WithUserId_ShouldReturnUserSummary()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateParentUserAsync(userId);
        var email = $"test{DateTime.UtcNow.Ticks}@example.com";
        
        await using var conn = new NpgsqlConnection(_connectionString);
        
        // Insert test business
        var businessId = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO business (id, name, business_citytown, business_state, business_address, is_verified)
            VALUES (@Id, @Name, @City, @State, @Address, @IsVerified)
            ON CONFLICT (id) DO NOTHING",
            new { Id = businessId, Name = "Test Business", 
                  City = "Test City", State = "TS", Address = "123 Test St", IsVerified = true });

        // Insert test reviews
        var reviewId = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO review (id, business_id, reviewer_id, email, star_rating, review_body, status, created_at)
            VALUES (@Id, @BusinessId, @ReviewerId, @Email, @StarRating, @ReviewBody, @Status, @CreatedAt)",
            new { Id = reviewId, BusinessId = businessId, ReviewerId = userId, Email = email,
                  StarRating = 5m, ReviewBody = "This is a great place with excellent service and wonderful atmosphere. Highly recommended!", Status = "APPROVED", CreatedAt = DateTime.UtcNow });

        // Insert user badges
        await conn.ExecuteAsync(@"
            INSERT INTO user_badges (id, user_id, badge_type, earned_at, is_active)
            VALUES (@Id, @UserId, @BadgeType, @EarnedAt, @IsActive)",
            new { Id = Guid.NewGuid(), UserId = userId, BadgeType = "Newbie", 
                  EarnedAt = DateTime.UtcNow, IsActive = true });

        // Insert user points
        await conn.ExecuteAsync(@"
            INSERT INTO user_points (user_id, total_points, current_streak, updated_at)
            VALUES (@UserId, @TotalPoints, @CurrentStreak, @UpdatedAt)
            ON CONFLICT (user_id) DO UPDATE SET total_points = @TotalPoints",
            new { UserId = userId, TotalPoints = 100, CurrentStreak = 5, UpdatedAt = DateTime.UtcNow });

        // Insert point transactions
        await conn.ExecuteAsync(@"
            INSERT INTO point_transactions (id, user_id, points, transaction_type, description, created_at)
            VALUES (@Id, @UserId, @Points, @Type, @Description, @CreatedAt)",
            new { Id = Guid.NewGuid(), UserId = userId, Points = 50, Type = "EARNED", 
                  Description = "Wrote a review", CreatedAt = DateTime.UtcNow.AddHours(-1) });

        // Act
        var result = await _repository.GetUserDataAsync(userId, null);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserId, Is.EqualTo(userId));
        Assert.That(result.Reviews, Is.Not.Empty);
        
        var review = result.Reviews.First();
        Assert.That(review.StarRating, Is.EqualTo(5m));
        Assert.That(review.Name, Is.EqualTo("Test Business"));
        
        Assert.That(result.Badges, Is.Not.Empty);
        Assert.That(result.Badges.First().BadgeType, Is.EqualTo("Newbie"));
        
        Assert.That(result.Points, Is.EqualTo(100));
        Assert.That(result.Streak, Is.EqualTo(5));
        Assert.That(result.Rank, Is.GreaterThan(0));
        Assert.That(result.LifetimePoints, Is.EqualTo(50)); // From the transaction
        
        Assert.That(result.RecentActivity, Is.Not.Empty);
        Assert.That(result.RecentActivity.First().Points, Is.EqualTo(50));
    }

    [Test]
    public async Task GetUserDataAsync_WithEmail_ShouldReturnUserSummary()
    {
        // Arrange
        var email = $"test{DateTime.UtcNow.Ticks}@example.com";
        
        await using var conn = new NpgsqlConnection(_connectionString);
        
        // Insert test business
        var businessId = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO business (id, name, business_citytown, business_state, is_verified)
            VALUES (@Id, @Name, @City, @State, @IsVerified)
            ON CONFLICT (id) DO NOTHING",
            new { Id = businessId, Name = "Email Test Business", 
                  City = "Email City", State = "EC", IsVerified = true });

        // Insert test reviews (guest review with email)
        var reviewId = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO review (id, business_id, email, star_rating, review_body, status, is_guest_review, created_at)
            VALUES (@Id, @BusinessId, @Email, @StarRating, @ReviewBody, @Status, @IsGuest, @CreatedAt)",
            new { Id = reviewId, BusinessId = businessId, Email = email,
                  StarRating = 4m, ReviewBody = "Had a really nice experience visiting this place as a guest. The staff were friendly and helpful throughout.", Status = "APPROVED", 
                  IsGuest = true, CreatedAt = DateTime.UtcNow });

        // Act
        var result = await _repository.GetUserDataAsync(null, email);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Email, Is.EqualTo(email));
        Assert.That(result.Reviews, Is.Not.Empty);
        
        var review = result.Reviews.First();
        Assert.That(review.StarRating, Is.EqualTo(4m));
        Assert.That(review.IsGuestReview, Is.True);
        
        // Guest reviews shouldn't have badges or points
        Assert.That(result.Badges, Is.Empty);
        Assert.That(result.Points, Is.EqualTo(0));
    }

    [Test]
    public async Task GetUserDataAsync_WithNoIdentifier_ShouldReturnEmptySummary()
    {
        // Act
        var result = await _repository.GetUserDataAsync(null, null);

        // Assert
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
        // Act
        var result = await _repository.GetUserDataAsync(Guid.NewGuid(), null);

        // Assert
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
        // Arrange
        var userId = Guid.NewGuid();
        await CreateParentUserAsync(userId);
        var email = $"test{DateTime.UtcNow.Ticks}@example.com";
        
        await using var conn = new NpgsqlConnection(_connectionString);
        
        // Insert test businesses in different cities
        var business1Id = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO business (id, name, business_citytown, business_state, is_verified)
            VALUES (@Id, @Name, @City, @State, @IsVerified)
            ON CONFLICT (id) DO NOTHING",
            new { Id = business1Id, Name = "Business 1", 
                  City = "New York", State = "NY", IsVerified = true });

        var business2Id = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO business (id, name, business_citytown, business_state, is_verified)
            VALUES (@Id, @Name, @City, @State, @IsVerified)
            ON CONFLICT (id) DO NOTHING",
            new { Id = business2Id, Name = "Business 2", 
                  City = "Los Angeles", State = "CA", IsVerified = true });

        // Insert categories
        var category1Id = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO category (id, name) VALUES (@Id, @Name)
            ON CONFLICT (id) DO NOTHING",
            new { Id = category1Id, Name = "Restaurant" });

        var category2Id = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO category (id, name) VALUES (@Id, @Name)
            ON CONFLICT (id) DO NOTHING",
            new { Id = category2Id, Name = "Cafe" });

        // Link businesses to categories
        await conn.ExecuteAsync(@"
            INSERT INTO business_category (business_id, category_id) VALUES (@BusinessId, @CategoryId)
            ON CONFLICT DO NOTHING",
            new[] 
            {
                new { BusinessId = business1Id, CategoryId = category1Id },
                new { BusinessId = business1Id, CategoryId = category2Id },
                new { BusinessId = business2Id, CategoryId = category2Id }
            });

        // Insert reviews with location_id (required by topCitiesSql: location_id IS NOT NULL)
        var branch1Id = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO business_branches (id, business_id, branch_citytown, branch_state)
            VALUES (@Id, @BusinessId, @City, @State)",
            new { Id = branch1Id, BusinessId = business1Id, City = "New York", State = "NY" });

        var branch2Id = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO business_branches (id, business_id, branch_citytown, branch_state)
            VALUES (@Id, @BusinessId, @City, @State)",
            new { Id = branch2Id, BusinessId = business2Id, City = "Los Angeles", State = "CA" });

        var now = DateTime.UtcNow;
        for (int i = 0; i < 3; i++)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO review (id, business_id, location_id, reviewer_id, email, star_rating, review_body, status, created_at)
                VALUES (@Id, @BusinessId, @LocationId, @ReviewerId, @Email, @StarRating, @ReviewBody, @Status, @CreatedAt)",
                new 
                { 
                    Id = Guid.NewGuid(), 
                    BusinessId = business1Id,
                    LocationId = branch1Id,
                    ReviewerId = userId, 
                    Email = email,
                    StarRating = 5m, 
                    ReviewBody = "Really enjoyed my visit here. The food was delicious and the service was top notch. Would definitely come back again!", 
                    Status = "APPROVED", 
                    CreatedAt = now.AddDays(-i) 
                });
        }

        for (int i = 0; i < 2; i++)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO review (id, business_id, location_id, reviewer_id, email, star_rating, review_body, status, created_at)
                VALUES (@Id, @BusinessId, @LocationId, @ReviewerId, @Email, @StarRating, @ReviewBody, @Status, @CreatedAt)",
                new 
                { 
                    Id = Guid.NewGuid(), 
                    BusinessId = business2Id,
                    LocationId = branch2Id,
                    ReviewerId = userId, 
                    Email = email,
                    StarRating = 4m, 
                    ReviewBody = "A solid experience overall. The ambiance was pleasant and the quality was consistently good. Worth a visit!", 
                    Status = "APPROVED", 
                    CreatedAt = now.AddDays(-(3 + i)) 
                });
        }

        // Act
        var result = await _repository.GetUserDataAsync(userId, null);

        // Assert
        Assert.That(result.Reviews.Count(), Is.EqualTo(5));
        
        Assert.That(result.TopCities, Is.Not.Empty);
        Assert.That(result.TopCities.Count(), Is.LessThanOrEqualTo(3));
        
        // New York should be first (3 reviews)
        var topCity = result.TopCities.First();
        Assert.That(topCity.City, Is.EqualTo("New York"));
        Assert.That(topCity.ReviewCount, Is.EqualTo(3));
        
        Assert.That(result.TopCategories, Is.Not.Empty);
        // Cafe appears in both businesses, so it should be first
        var topCategory = result.TopCategories.First();
        Assert.That(topCategory.CategoryName, Is.EqualTo("Cafe"));
        Assert.That(topCategory.ReviewCount, Is.EqualTo(5)); // All reviews are in Cafe category
    }

    [Test]
    public async Task GetUserDataAsync_WithBadges_ShouldReturnActiveBadgesOnly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateParentUserAsync(userId);

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
                Id1 = Guid.NewGuid(), UserId = userId, BadgeType1 = "Newbie", EarnedAt1 = DateTime.UtcNow.AddDays(-10),
                Id2 = Guid.NewGuid(), BadgeType2 = "Expert", EarnedAt2 = DateTime.UtcNow.AddDays(-5),
                Id3 = Guid.NewGuid(), BadgeType3 = "Pro", EarnedAt3 = DateTime.UtcNow
            });

        // Act
        var result = await _repository.GetUserDataAsync(userId, null);

        // Assert
        Assert.That(result.Badges.Count(), Is.EqualTo(2));
        Assert.That(result.Badges.Any(b => b.BadgeType == "Newbie"), Is.True);
        Assert.That(result.Badges.Any(b => b.BadgeType == "Expert"), Is.True);
        Assert.That(result.Badges.Any(b => b.BadgeType == "Pro"), Is.False);
        Assert.That(result.CurrentTier, Is.Not.Null); // Should be set to the highest tier badge
    }

    [Test]
    public async Task GetUserDataAsync_WithPoints_ShouldCalculateCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await CreateParentUserAsync(userId);

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
            new { UserId = userId, TotalPoints = 250, CurrentStreak = 10, UpdatedAt = DateTime.UtcNow });

        // Insert multiple point transactions
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
        Assert.That(result.LifetimePoints, Is.EqualTo(175)); // Sum of all transactions (100+50+25)
        Assert.That(result.RecentActivity.Count(), Is.EqualTo(3));
        Assert.That(result.RecentActivity.First().TransactionType, Is.EqualTo("REDEEMED")); // Most recent first
        Assert.That(result.RecentActivity.First().Points, Is.EqualTo(25));
    }
}