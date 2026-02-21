using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure.Tests.Repositories;

[TestFixture]
public class UserRepositoryTests
{
    // ✅ GLOBAL TEST IDENTIFIER
    private const string InfrastructureTestPrefix = "__INFRA_TEST__";

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

        // ✅ ONLY delete test users
        await conn.ExecuteAsync(@"
            DELETE FROM users
            WHERE username LIKE @prefix
               OR email LIKE @prefix;
        ", new { prefix = InfrastructureTestPrefix + "%" });
    }

    [Test]
    public async Task AddAsync_ShouldInsertUser_AndGetById_ShouldReturnUser()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..6];

        var user = new User(
            $"{InfrastructureTestPrefix}_user_{unique}",
            $"{InfrastructureTestPrefix}_{unique}@t.c",
            "1234567890",
            "password123",
            "end_user",
            "123 Main St",
            "auth0|test"
        );

        // Act
        await _repository.AddAsync(user);
        var fetched = await _repository.GetByIdAsync(user.Id);

        // Assert
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Username, Is.EqualTo(user.Username));
        Assert.That(fetched.Email, Is.EqualTo(user.Email));
        Assert.That(fetched.Phone, Is.EqualTo("1234567890"));
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnUsers_WhenUsersExist()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..6];

        var user1 = new User(
            $"{InfrastructureTestPrefix}_user1_{unique}",
            $"{InfrastructureTestPrefix}_u1_{unique}@t.c",
            "1111111111",
            "password123",
            "end_user",
            "Addr1",
            "auth0|1"
        );

        var user2 = new User(
            $"{InfrastructureTestPrefix}_user2_{unique}",
            $"{InfrastructureTestPrefix}_u2_{unique}@t.c",
            "2222222222",
            "password123",
            "end_user",
            "Addr2",
            "auth0|2"
        );

        await _repository.AddAsync(user1);
        await _repository.AddAsync(user2);

        // Act
        var results = (await _repository.GetAllAsync()).ToList();

        // Assert
        Assert.That(results.Any(u => u.Username == user1.Username), Is.True);
        Assert.That(results.Any(u => u.Username == user2.Username), Is.True);
    }
}
