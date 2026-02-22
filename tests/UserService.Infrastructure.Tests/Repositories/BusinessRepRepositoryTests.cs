using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;
using NUnit.Framework;

namespace UserService.Infrastructure.Tests.Repositories;

[TestFixture]
public class BusinessRepRepositoryTests
{
    private const string InfrastructureTestPrefix = "__INFRA_TEST__";
    private static string Unique() => Guid.NewGuid().ToString("N")[..6];

    private BusinessRepRepository _repository = null!;
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

        _repository = new BusinessRepRepository(_configuration);
        _userRepository = new UserRepository(_configuration);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var requiredTables = new[] { "business_reps", "users" };
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
        var prefix = InfrastructureTestPrefix + "%";

        await conn.ExecuteAsync(@"
            DELETE FROM business_reps
            WHERE user_id IN (
                SELECT id FROM users WHERE username LIKE @prefix OR email LIKE @prefix
            );", new { prefix });

        await conn.ExecuteAsync(@"
            DELETE FROM users
            WHERE username LIKE @prefix OR email LIKE @prefix;", new { prefix });
    }

    private async Task<User> CreateBusinessUserAsync(string? username = null)
    {
        var unique = Unique();
        var user = new User(
            username ?? $"{InfrastructureTestPrefix}_rep_{unique}",
            $"{InfrastructureTestPrefix}_{unique}@biz.com",
            "1234567890",
            "123456",
            "business_user",
            "Test Address",
            "test"
        );

        await _userRepository.AddAsync(user);
        return user;
    }

    [Test]
    public async Task AddAsync_ShouldInsertAndRetrieveBusinessRep()
    {
        var user = await CreateBusinessUserAsync();
        var businessId = Guid.NewGuid();

        var businessRep = new BusinessRep(businessId, user.Id, "Branch A", "Address A");

        await _repository.AddAsync(businessRep);
        var fetched = await _repository.GetByIdAsync(businessRep.Id);

        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.UserId, Is.EqualTo(user.Id));
        Assert.That(fetched.BusinessId, Is.EqualTo(businessId));
        Assert.That(fetched.BranchName, Is.EqualTo("Branch A"));
    }

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnBusinessRep_WhenExists()
    {
        var user = await CreateBusinessUserAsync();

        var businessRep = new BusinessRep(Guid.NewGuid(), user.Id, "Branch X", "Street X");
        await _repository.AddAsync(businessRep);

        var result = await _repository.GetByUserIdAsync(user.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.UserId, Is.EqualTo(user.Id));
        Assert.That(result.BranchName, Is.EqualTo("Branch X"));
    }

    [Test]
    public async Task UpdateAsync_ShouldModifyBranchInfo()
    {
        var user = await CreateBusinessUserAsync();

        var businessRep = new BusinessRep(Guid.NewGuid(), user.Id, "Old Branch", "Old Location");
        await _repository.AddAsync(businessRep);

        businessRep.UpdateBranch("New Branch", "New Location");
        await _repository.UpdateAsync(businessRep);

        var updated = await _repository.GetByIdAsync(businessRep.Id);

        Assert.That(updated!.BranchName, Is.EqualTo("New Branch"));
        Assert.That(updated.BranchAddress, Is.EqualTo("New Location"));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveBusinessRep()
    {
        var user = await CreateBusinessUserAsync();

        var businessRep = new BusinessRep(Guid.NewGuid(), user.Id, "Del Branch", "Del Address");
        await _repository.AddAsync(businessRep);

        await _repository.DeleteAsync(businessRep.Id);
        var result = await _repository.GetByIdAsync(businessRep.Id);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByBusinessIdAsync_ShouldReturnAllRepsForBusiness()
    {
        var businessId = Guid.NewGuid();

        var user1 = await CreateBusinessUserAsync();
        var user2 = await CreateBusinessUserAsync();

        await _repository.AddAsync(new BusinessRep(businessId, user1.Id, "Branch 1", "Location 1"));
        await _repository.AddAsync(new BusinessRep(businessId, user2.Id, "Branch 2", "Location 2"));

        var results = (await _repository.GetByBusinessIdAsync(businessId)).ToList();

        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results.Any(r => r.BranchName == "Branch 1"), Is.True);
        Assert.That(results.Any(r => r.BranchName == "Branch 2"), Is.True);
    }

    [Test]
    public async Task UpdateAsync_WithUserDetails_ShouldModifyBothUserAndBusinessRep()
    {
        var user = await CreateBusinessUserAsync();

        var businessId = Guid.NewGuid();
        var businessRep = new BusinessRep(businessId, user.Id, "Initial Branch", "Initial Address");
        await _repository.AddAsync(businessRep);

        user.Update(
            $"{InfrastructureTestPrefix}_updated_{Unique()}@biz.com",
            "7777777777",
            "Updated Address"
        );

        await _userRepository.UpdateAsync(user);

        businessRep.UpdateBranch("Updated Branch", "Updated Branch Address");
        await _repository.UpdateAsync(businessRep);

        var updatedUser = await _userRepository.GetByIdAsync(user.Id);
        var updatedBusinessRep = await _repository.GetByIdAsync(businessRep.Id);

        Assert.Multiple(() =>
        {
            Assert.That(updatedUser, Is.Not.Null);
            Assert.That(updatedUser!.Email, Does.StartWith(InfrastructureTestPrefix));
            Assert.That(updatedUser.Phone, Is.EqualTo("7777777777"));
            Assert.That(updatedUser.Address, Is.EqualTo("Updated Address"));

            Assert.That(updatedBusinessRep, Is.Not.Null);
            Assert.That(updatedBusinessRep!.BranchName, Is.EqualTo("Updated Branch"));
            Assert.That(updatedBusinessRep.BranchAddress, Is.EqualTo("Updated Branch Address"));
            Assert.That(updatedBusinessRep.UserId, Is.EqualTo(user.Id));
        });
    }

    [Test]
    public async Task GetParentRepByBusinessIdAsync_ShouldReturnEarliestRep_WhenMultipleRepsExist()
    {
        var businessId = Guid.NewGuid();

        var user1 = await CreateBusinessUserAsync();
        var user2 = await CreateBusinessUserAsync();
        var user3 = await CreateBusinessUserAsync();

        var parentRep = new BusinessRep(businessId, user1.Id, "Parent Branch", "Parent Location");
        await _repository.AddAsync(parentRep);

        await Task.Delay(100);

        await _repository.AddAsync(new BusinessRep(businessId, user2.Id, "Child Branch 1", "Child Location 1"));
        await _repository.AddAsync(new BusinessRep(businessId, user3.Id, "Child Branch 2", "Child Location 2"));

        var result = await _repository.GetParentRepByBusinessIdAsync(businessId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.UserId, Is.EqualTo(user1.Id));
        Assert.That(result.BranchName, Is.EqualTo("Parent Branch"));
        Assert.That(result.Id, Is.EqualTo(parentRep.Id));
    }

    [Test]
    public async Task GetParentRepByBusinessIdAsync_ShouldReturnNull_WhenNoRepsExist()
    {
        var businessId = Guid.NewGuid();

        var result = await _repository.GetParentRepByBusinessIdAsync(businessId);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetParentRepByBusinessIdAsync_ShouldReturnOnlyRep_WhenSingleRepExists()
    {
        var businessId = Guid.NewGuid();
        var user = await CreateBusinessUserAsync();

        var businessRep = new BusinessRep(businessId, user.Id, "Only Branch", "Only Location");
        await _repository.AddAsync(businessRep);

        var result = await _repository.GetParentRepByBusinessIdAsync(businessId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(businessRep.Id));
        Assert.That(result.UserId, Is.EqualTo(user.Id));
        Assert.That(result.BranchName, Is.EqualTo("Only Branch"));
    }
}
