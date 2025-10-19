using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure.Tests.Repositories;

[TestFixture]
public class UserRepositoryTests
{
    private UserRepository _repository = null!;
    private IConfiguration _configuration = null!;
    private string _connectionString = string.Empty;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        // ✅ Load configuration from appsettings.json
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

        _configuration = builder.Build();
        _connectionString = _configuration.GetConnectionString("PostgresConnection")
                            ?? throw new InvalidOperationException("Missing connection string in appsettings.json");

        _repository = new UserRepository(_configuration);

        // ✅ Ensure the "users" table exists before running tests
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var tableExists = await conn.ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT FROM information_schema.tables WHERE table_name = 'users'
            );
        ");

        if (!tableExists)
            Assert.Fail("❌ Table 'users' does not exist. Run migrations before running tests.");
    }

    [SetUp]
    public async Task Setup()
    {
        // ✅ Clean up test data before each test
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync("DELETE FROM users WHERE user_type = 'business_user';");
    }

    // ✅ Test: Add + GetById
    [Test]
    public async Task AddAsync_ShouldInsertUser_AndGetById_ShouldReturnUser()
    {
        // Arrange
        var user = new User("test_user", "test_user@domain.com", "1234567890", "business_user", "123 Test Street");

        // Act
        await _repository.AddAsync(user);
        var fetched = await _repository.GetByIdAsync(user.Id);

        // Assert
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Username, Is.EqualTo("test_user"));
        Assert.That(fetched.Email, Is.EqualTo("test_user@domain.com"));
        Assert.That(fetched.UserType, Is.EqualTo("business_user"));
    }

    // ✅ Test: GetAllAsync returns users
    [Test]
    public async Task GetAllAsync_ShouldReturnUsers_WhenUsersExist()
    {
        // Arrange
        var user1 = new User("user1", "user1@domain.com", "1111111111", "business_user", "Address 1");
        var user2 = new User("user2", "user2@domain.com", "2222222222", "business_user", "Address 2");

        await _repository.AddAsync(user1);
        await _repository.AddAsync(user2);

        // Act
        var allUsers = (await _repository.GetAllAsync()).ToList();

        // Assert
        Assert.That(allUsers.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(allUsers.Any(u => u.Username == "user1"), Is.True);
        Assert.That(allUsers.Any(u => u.Username == "user2"), Is.True);
    }

    // ✅ Test: Delete user
    [Test]
    public async Task DeleteAsync_ShouldRemoveUser()
    {
        // Arrange
        var user = new User("delete_user", "delete@domain.com", "4444444444", "business_user", "Delete Address");
        await _repository.AddAsync(user);

        // Act
        await _repository.DeleteAsync(user.Id);
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        Assert.That(result, Is.Null);
    }

    // ✅ Test: GetByIdAsync returns null when not found
    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Null);
    }
}
