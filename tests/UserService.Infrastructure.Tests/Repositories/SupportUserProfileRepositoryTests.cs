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
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

        _configuration = builder.Build();
        _connectionString = _configuration.GetConnectionString("PostgresConnection")
                            ?? throw new InvalidOperationException("Missing connection string in appsettings.json");

        _repository = new SupportUserProfileRepository(_configuration);
        _userRepository = new UserRepository(_configuration);

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
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync("DELETE FROM support_user;");
        await conn.ExecuteAsync("DELETE FROM users WHERE user_type = 'support_user';");
    }

    // ✅ FIXED: Use email under 20 characters for varchar(20) constraint
    [Test]
    public async Task AddAsync_ShouldInsertAndRetrieveSupportUserProfile()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.Ticks;
        var user = new User("support_user", $"sup{timestamp % 1000000}@t.c", "3333333333", "password123", "support_user", "User St", "test");

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

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnSupportProfile_WhenExists()
    {
        var timestamp = DateTime.UtcNow.Ticks;
        var user = new User("support_user", $"s{timestamp % 10000}@t.c", "3333333333", "password123", "support_user", "User St", "test");
        await _userRepository.AddAsync(user);

        var supportProfile = new SupportUserProfile(user.Id);
        await _repository.AddAsync(supportProfile);

        var result = await _repository.GetByUserIdAsync(user.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.UserId, Is.EqualTo(user.Id));
    }

    [Test]
    public async Task UpdateAsync_ShouldModifyTimestamp()
    {
        var timestamp = DateTime.UtcNow.Ticks;
        var user = new User("support_update", $"up{timestamp % 10000}@t.c", "4444444444", "password123", "support_user", "Addr", "test");
        await _userRepository.AddAsync(user);

        var supportProfile = new SupportUserProfile(user.Id);
        await _repository.AddAsync(supportProfile);

        var originalUpdatedAt = supportProfile.UpdatedAt;
        
        await Task.Delay(100);
        
        supportProfile.UpdateTimestamp();
        await _repository.UpdateAsync(supportProfile);

        var updated = await _repository.GetByIdAsync(supportProfile.Id);
        Assert.That(updated!.UpdatedAt, Is.GreaterThan(originalUpdatedAt));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveSupportProfile()
    {
        var timestamp = DateTime.UtcNow.Ticks;
        var user = new User("support_delete", $"del{timestamp % 10000}@t.c", "5555555555", "password123", "support_user", "Addr D", "test");
        await _userRepository.AddAsync(user);

        var supportProfile = new SupportUserProfile(user.Id);
        await _repository.AddAsync(supportProfile);

        await _repository.DeleteAsync(supportProfile.Id);
        var result = await _repository.GetByIdAsync(supportProfile.Id);

        Assert.That(result, Is.Null);
    }

    // ✅ FIXED: Use unique emails for each user
    [Test]
    public async Task GetAllAsync_ShouldReturnAllSupportProfiles()
    {
        var timestamp = DateTime.UtcNow.Ticks;
        var user1 = new User("support1", $"s1{timestamp}@t.c", "1111111111", "password123", "support_user", "Addr1", "test");
        var user2 = new User("support2", $"s2{timestamp}@t.c", "2222222222", "password123", "support_user", "Addr2", "test");
        await _userRepository.AddAsync(user1);
        await _userRepository.AddAsync(user2);

        await _repository.AddAsync(new SupportUserProfile(user1.Id));
        await _repository.AddAsync(new SupportUserProfile(user2.Id));

        var results = (await _repository.GetAllAsync()).ToList();

        Assert.That(results.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(results.Any(r => r.UserId == user1.Id), Is.True);
        Assert.That(results.Any(r => r.UserId == user2.Id), Is.True);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenProfileDoesNotExist()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnNull_WhenProfileDoesNotExist()
    {
        var result = await _repository.GetByUserIdAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DeleteUser_ShouldCascadeDeleteSupportProfile()
    {
        var timestamp = DateTime.UtcNow.Ticks;
        var user = new User("cascade_test", $"cas{timestamp}@t.c", "6666666666", "password123", "support_user", "Cascade St", "test");
        await _userRepository.AddAsync(user);

        var supportProfile = new SupportUserProfile(user.Id);
        await _repository.AddAsync(supportProfile);

        await _userRepository.DeleteAsync(user.Id);

        var result = await _repository.GetByIdAsync(supportProfile.Id);
        Assert.That(result, Is.Null, "Support profile should be cascade deleted when user is deleted");
    }

    [Test]
    public async Task UpdateAsync_ShouldPersistMultipleTimestampUpdates()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.Ticks;
        var user = new User("multi_update", $"mu{timestamp}@t.c", "1111111111", "password123", "support_user", "Multi St", "auth0|test");
        await _userRepository.AddAsync(user);

        var supportProfile = new SupportUserProfile(user.Id);
        await _repository.AddAsync(supportProfile);

        var originalUpdatedAt = supportProfile.UpdatedAt;

        // Act - First update
        await Task.Delay(100);
        supportProfile.UpdateTimestamp();
        await _repository.UpdateAsync(supportProfile);
        var firstUpdate = await _repository.GetByIdAsync(supportProfile.Id);

        // Act - Second update
        await Task.Delay(100);
        supportProfile.UpdateTimestamp();
        await _repository.UpdateAsync(supportProfile);
        var secondUpdate = await _repository.GetByIdAsync(supportProfile.Id);

        // Assert
        Assert.That(firstUpdate!.UpdatedAt, Is.GreaterThan(originalUpdatedAt));
        Assert.That(secondUpdate!.UpdatedAt, Is.GreaterThan(firstUpdate.UpdatedAt));
    }

 
    [Test]
    public async Task UpdateAsync_ShouldNotModifyCreatedAt()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.Ticks;
        var user = new User("created_check", $"cc{timestamp}@t.c", "2222222222", "password123", "support_user", "Created St", "auth0|test");
        await _userRepository.AddAsync(user);

        var supportProfile = new SupportUserProfile(user.Id);
        await _repository.AddAsync(supportProfile);

        // ✅ FIXED: Get the CreatedAt value from the database after insert
        var fetchedAfterInsert = await _repository.GetByIdAsync(supportProfile.Id);
        var originalCreatedAt = fetchedAfterInsert!.CreatedAt;

        // Act
        await Task.Delay(100);
        supportProfile.UpdateTimestamp();
        await _repository.UpdateAsync(supportProfile);

        // Assert
        var updated = await _repository.GetByIdAsync(supportProfile.Id);

        // ✅ FIXED: Convert both to UTC before comparing
        DateTime ToUtc(DateTime dt) => DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        DateTime RoundMs(DateTime dt) => new DateTime(dt.Ticks - (dt.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);

        Assert.Multiple(() =>
        {
            Assert.That(RoundMs(ToUtc(updated!.CreatedAt)), Is.EqualTo(RoundMs(ToUtc(originalCreatedAt))));
            Assert.That(RoundMs(ToUtc(updated.UpdatedAt)), Is.GreaterThan(RoundMs(ToUtc(originalCreatedAt))));
        });
    }
    

    [Test]
    public async Task UpdateAsync_WithNonExistentId_ShouldNotThrowException()
    {
        // Arrange
        var nonExistentProfile = new SupportUserProfile(Guid.NewGuid());

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _repository.UpdateAsync(nonExistentProfile));
    }

    [Test]
    public async Task UpdateAsync_ConcurrentUpdates_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.Ticks;
        var user = new User("concurrent_test", $"con{timestamp}@t.c", "3333333333", "password123", "support_user", "Concurrent St", "auth0|test");
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
                supportProfile.UpdateTimestamp();
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
