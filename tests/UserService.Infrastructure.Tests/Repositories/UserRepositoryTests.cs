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
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

        _configuration = builder.Build();
        _connectionString = _configuration.GetConnectionString("PostgresConnection")
                            ?? throw new InvalidOperationException("Missing connection string");

        _repository = new UserRepository(_configuration);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var exists = await conn.ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT FROM information_schema.tables WHERE table_name = 'users'
            );
        ");

        if (!exists)
            Assert.Fail("❌ Table 'users' does not exist. Run migrations first.");
    }

    [SetUp]
    public async Task Setup()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync("DELETE FROM users;");
    }

    // ✅ FIXED: Use email under 20 characters
    [Test]
    public async Task AddAsync_ShouldInsertUser_AndGetById_ShouldReturnUser()
    {
        // Arrange
        var user = new User("test_user", "test@test.com", "1234567890", "password123", "end_user", "123 Main St", "auth0|test");

        // Act
        await _repository.AddAsync(user);
        var fetched = await _repository.GetByIdAsync(user.Id);

        // Assert
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Username, Is.EqualTo("test_user"));
        Assert.That(fetched.Email, Is.EqualTo("test@test.com"));
        Assert.That(fetched.Phone, Is.EqualTo("1234567890"));
    }

    // ✅ FIXED: Use unique emails for each user
    [Test]
    public async Task GetAllAsync_ShouldReturnUsers_WhenUsersExist()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.Ticks;
        var user1 = new User("user1", $"u1{timestamp}@t.c", "1111111111", "password123", "end_user", "Addr1", "auth0|1");
        var user2 = new User("user2", $"u2{timestamp}@t.c", "2222222222", "password123", "end_user", "Addr2", "auth0|2");
        
        await _repository.AddAsync(user1);
        await _repository.AddAsync(user2);

        // Act
        var results = (await _repository.GetAllAsync()).ToList();

        // Assert
        Assert.That(results.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(results.Any(u => u.Username == "user1"), Is.True);
        Assert.That(results.Any(u => u.Username == "user2"), Is.True);
    }
}