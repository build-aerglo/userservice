using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure.Tests.Repositories;

[TestFixture]
public class SupportUserProfileRepositoryTests
{
    private SupportUserProfileRepository _repository = null!;
    private UserRepository _userRepository = null!;
    private IConfiguration _configuration = null!;
    private string _connectionString = string.Empty;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        // ✅ Load appsettings.json for the real connection string
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

        _configuration = builder.Build();
        _connectionString = _configuration.GetConnectionString("PostgresConnection")
                            ?? throw new InvalidOperationException("Missing connection string in appsettings.json");

        _repository = new SupportUserProfileRepository(_configuration);
        _userRepository = new UserRepository(_configuration);

        // ✅ Ensure tables exist before running tests
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var requiredTables = new[] { "support_user", "users" };
        foreach (var table in requiredTables)
        {
            var exists = await conn.ExecuteScalarAsync<bool>($@"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables WHERE table_name = '{table}'
                );
            ");

            if (!exists)
                Assert.Fail($"❌ Table '{table}' does not exist. Run migrations first.");
        }
    }

    [SetUp]
    public async Task Setup()
    {
        // ✅ Clean test data before each test
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync("DELETE FROM support_user;");
        await conn.ExecuteAsync("DELETE FROM users WHERE user_type = 'support_user';");
    }

    // ✅ Test: Add and retrieve a support user profile
    [Test]
    public async Task AddAsync_ShouldInsertAndRetrieveSupportUserProfile()
    {
        // Arrange
        var user = new User("support_add", "support_add@admin.com", "1234567890", "support_user", "123 Admin St");
        await _userRepository.AddAsync(user);

        var supportProfile = new SupportUserProfile(user.Id);

        // Act
        await _repository.AddAsync(supportProfile);
        var fetched = await _repository.GetByIdAsync(supportProfile.Id);

        // Assert
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.UserId, Is.EqualTo(user.Id));
        Assert.That(fetched.Id, Is.EqualTo(supportProfile.Id));
    }

    // ✅ Test: GetByUserId returns the right profile
    [Test]
    public async Task GetByUserIdAsync_ShouldReturnSupportProfile_WhenExists()
    {
        var user = new User("support_user", "support_user@admin.com", "3333333333", "support_user", "User St");
        await _userRepository.AddAsync(user);

        var supportProfile = new SupportUserProfile(user.Id);
        await _repository.AddAsync(supportProfile);

        var result = await _repository.GetByUserIdAsync(user.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.UserId, Is.EqualTo(user.Id));
    }

    // ✅ Test: Update support profile (touch timestamp)
    [Test]
    public async Task UpdateAsync_ShouldModifyTimestamp()
    {
        var user = new User("support_update", "support_update@admin.com", "4444444444", "support_user", "Addr");
        await _userRepository.AddAsync(user);

        var supportProfile = new SupportUserProfile(user.Id);
        await _repository.AddAsync(supportProfile);

        var originalUpdatedAt = supportProfile.UpdatedAt;
        
        // Wait a bit to ensure timestamp difference
        await Task.Delay(100);
        
        supportProfile.Touch();
        await _repository.UpdateAsync(supportProfile);

        var updated = await _repository.GetByIdAsync(supportProfile.Id);
        Assert.That(updated!.UpdatedAt, Is.GreaterThan(originalUpdatedAt));
    }

    // ✅ Test: Delete
    [Test]
    public async Task DeleteAsync_ShouldRemoveSupportProfile()
    {
        var user = new User("support_delete", "support_delete@admin.com", "5555555555", "support_user", "Addr D");
        await _userRepository.AddAsync(user);

        var supportProfile = new SupportUserProfile(user.Id);
        await _repository.AddAsync(supportProfile);

        await _repository.DeleteAsync(supportProfile.Id);
        var result = await _repository.GetByIdAsync(supportProfile.Id);

        Assert.That(result, Is.Null);
    }

    // ✅ Test: GetAllAsync returns all support profiles
    [Test]
    public async Task GetAllAsync_ShouldReturnAllSupportProfiles()
    {
        var user1 = new User("support1", "support1@admin.com", "1111111111", "support_user", "Addr1");
        var user2 = new User("support2", "support2@admin.com", "2222222222", "support_user", "Addr2");
        await _userRepository.AddAsync(user1);
        await _userRepository.AddAsync(user2);

        await _repository.AddAsync(new SupportUserProfile(user1.Id));
        await _repository.AddAsync(new SupportUserProfile(user2.Id));

        var results = (await _repository.GetAllAsync()).ToList();

        Assert.That(results.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(results.Any(r => r.UserId == user1.Id), Is.True);
        Assert.That(results.Any(r => r.UserId == user2.Id), Is.True);
    }

    // ✅ Test: GetByIdAsync returns null when not found
    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenProfileDoesNotExist()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    // ✅ Test: GetByUserIdAsync returns null when not found
    [Test]
    public async Task GetByUserIdAsync_ShouldReturnNull_WhenProfileDoesNotExist()
    {
        var result = await _repository.GetByUserIdAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    // ✅ Test: Cascade delete when user is deleted
    [Test]
    public async Task DeleteUser_ShouldCascadeDeleteSupportProfile()
    {
        var user = new User("cascade_test", "cascade@admin.com", "6666666666", "support_user", "Cascade St");
        await _userRepository.AddAsync(user);

        var supportProfile = new SupportUserProfile(user.Id);
        await _repository.AddAsync(supportProfile);

        // Delete the user (should cascade to support_user)
        await _userRepository.DeleteAsync(user.Id);

        var result = await _repository.GetByIdAsync(supportProfile.Id);
        Assert.That(result, Is.Null, "Support profile should be cascade deleted when user is deleted");
    }

    //✅ Test: Update with multiple field changes
    [Test]
    public async Task UpdateAsync_ShouldPersistMultipleTimestampUpdates()
        {
            // Arrange
            var user = new User("multi_update", "multi@support.com", "1111111111", "support_user", "Multi St");
            await _userRepository.AddAsync(user);

            var supportProfile = new SupportUserProfile(user.Id);
            await _repository.AddAsync(supportProfile);

            var originalUpdatedAt = supportProfile.UpdatedAt;

            // Act - First update
            await Task.Delay(100);
            supportProfile.Touch();
            await _repository.UpdateAsync(supportProfile);
            var firstUpdate = await _repository.GetByIdAsync(supportProfile.Id);

            // Act - Second update
            await Task.Delay(100);
            supportProfile.Touch();
            await _repository.UpdateAsync(supportProfile);
            var secondUpdate = await _repository.GetByIdAsync(supportProfile.Id);

            // Assert
            Assert.That(firstUpdate!.UpdatedAt, Is.GreaterThan(originalUpdatedAt));
            Assert.That(secondUpdate!.UpdatedAt, Is.GreaterThan(firstUpdate.UpdatedAt));
        }

    // ✅ Test: Verify UpdateAsync doesn't modify CreatedAt
    [Test]
    public async Task UpdateAsync_ShouldNotModifyCreatedAt()
    {
        // Arrange
        var user = new User("created_check", "created@support.com", "2222222222", "support_user", "Created St");
        await _userRepository.AddAsync(user);

        var supportProfile = new SupportUserProfile(user.Id);
        await _repository.AddAsync(supportProfile);

        var originalCreatedAt = supportProfile.CreatedAt;

        // Act
        await Task.Delay(100); // ensure UpdatedAt will be different
        supportProfile.Touch();
        await _repository.UpdateAsync(supportProfile);

        // Assert
        var updated = await _repository.GetByIdAsync(supportProfile.Id);

        // Round both times to milliseconds to avoid precision mismatch
        DateTime RoundMs(DateTime dt) => new DateTime(dt.Ticks - (dt.Ticks % TimeSpan.TicksPerMillisecond), dt.Kind);

        Assert.Multiple(() =>
        {
            Assert.That(RoundMs(updated!.CreatedAt), Is.EqualTo(RoundMs(originalCreatedAt)));
            Assert.That(RoundMs(updated.UpdatedAt), Is.GreaterThan(RoundMs(originalCreatedAt)));
        });
    }

    // ✅ Test: Update non-existent profile should not throw exception
    [Test]
        public async Task UpdateAsync_WithNonExistentId_ShouldNotThrowException()
        {
            // Arrange
            var nonExistentProfile = new SupportUserProfile(Guid.NewGuid());

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _repository.UpdateAsync(nonExistentProfile));
        }

        // ✅ Test: Concurrent updates maintain data integrity
        [Test]
        public async Task UpdateAsync_ConcurrentUpdates_ShouldMaintainDataIntegrity()
        {
            // Arrange
            var user = new User("concurrent_test", "concurrent@support.com", "3333333333", "support_user", "Concurrent St");
            await _userRepository.AddAsync(user);

            var supportProfile = new SupportUserProfile(user.Id);
            await _repository.AddAsync(supportProfile);

            // Act - Simulate concurrent updates
            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await Task.Delay(10);
                    supportProfile.Touch();
                    await _repository.UpdateAsync(supportProfile);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            var result = await _repository.GetByIdAsync(supportProfile.Id);
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.UserId, Is.EqualTo(user.Id));
        }
}