using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure.Tests.Repositories;

[TestFixture]
public class BusinessRepRepositoryTests
{
    private BusinessRepRepository _repository = null!;
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

        _repository = new BusinessRepRepository(_configuration);
        _userRepository = new UserRepository(_configuration);

        // ✅ Ensure tables exist before running tests
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
        // ✅ Clean test data before each test
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync("DELETE FROM business_reps;");
        await conn.ExecuteAsync("DELETE FROM users WHERE user_type = 'business_user';");
    }

    // ✅ Test: Add and retrieve a business rep
    [Test]
    public async Task AddAsync_ShouldInsertAndRetrieveBusinessRep()
    {
        // Arrange
        var user = new User("rep_add", "rep_add@biz.com", "1234567890", "business_user", "123 Main St","test");
        await _userRepository.AddAsync(user);

        var businessId = Guid.NewGuid(); // Mocked business id (validated by BusinessService in reality)
        var businessRep = new BusinessRep(businessId, user.Id, "Branch A", "Address A");

        // Act
        await _repository.AddAsync(businessRep);
        var fetched = await _repository.GetByIdAsync(businessRep.Id);

        // Assert
        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.UserId, Is.EqualTo(user.Id));
        Assert.That(fetched.BusinessId, Is.EqualTo(businessId));
        Assert.That(fetched.BranchName, Is.EqualTo("Branch A"));
    }

    // ✅ Test: GetByUserId returns the right rep
    [Test]
    public async Task GetByUserIdAsync_ShouldReturnBusinessRep_WhenExists()
    {
        var user = new User("rep_user", "rep_user@biz.com", "3333333333", "business_user", "User St","test");
        await _userRepository.AddAsync(user);

        var businessRep = new BusinessRep(Guid.NewGuid(), user.Id, "Branch X", "Street X");
        await _repository.AddAsync(businessRep);

        var result = await _repository.GetByUserIdAsync(user.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.UserId, Is.EqualTo(user.Id));
        Assert.That(result.BranchName, Is.EqualTo("Branch X"));
    }

    // ✅ Test: Update branch info
    [Test]
    public async Task UpdateAsync_ShouldModifyBranchInfo()
    {
        var user = new User("rep_update", "rep_update@biz.com", "4444444444", "business_user", "Addr","test");
        await _userRepository.AddAsync(user);

        var businessRep = new BusinessRep(Guid.NewGuid(), user.Id, "Old Branch", "Old Location");
        await _repository.AddAsync(businessRep);

        businessRep.UpdateBranch("New Branch", "New Location");
        await _repository.UpdateAsync(businessRep);

        var updated = await _repository.GetByIdAsync(businessRep.Id);
        Assert.That(updated!.BranchName, Is.EqualTo("New Branch"));
        Assert.That(updated.BranchAddress, Is.EqualTo("New Location"));
    }

    // ✅ Test: Delete
    [Test]
    public async Task DeleteAsync_ShouldRemoveBusinessRep()
    {
        var user = new User("rep_delete", "rep_delete@biz.com", "5555555555", "business_user", "Addr D","test");
        await _userRepository.AddAsync(user);

        var businessRep = new BusinessRep(Guid.NewGuid(), user.Id, "Del Branch", "Del Address");
        await _repository.AddAsync(businessRep);

        await _repository.DeleteAsync(businessRep.Id);
        var result = await _repository.GetByIdAsync(businessRep.Id);

        Assert.That(result, Is.Null);
    }

    // ✅ Test: GetByBusinessId returns all reps for the same business
    [Test]
    public async Task GetByBusinessIdAsync_ShouldReturnAllRepsForBusiness()
    {
        var businessId = Guid.NewGuid();

        var user1 = new User("rep1", "rep1@biz.com", "1111111111", "business_user", "Addr1","test");
        var user2 = new User("rep2", "rep2@biz.com", "2222222222", "business_user", "Addr2","test");
        await _userRepository.AddAsync(user1);
        await _userRepository.AddAsync(user2);

        await _repository.AddAsync(new BusinessRep(businessId, user1.Id, "Branch 1", "Location 1"));
        await _repository.AddAsync(new BusinessRep(businessId, user2.Id, "Branch 2", "Location 2"));

        var results = (await _repository.GetByBusinessIdAsync(businessId)).ToList();

        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results.Any(r => r.BranchName == "Branch 1"), Is.True);
        Assert.That(results.Any(r => r.BranchName == "Branch 2"), Is.True);
    }

    // ✅ Test: Update with User details (Integration test pattern)
    [Test]
    public async Task UpdateAsync_WithUserDetails_ShouldModifyBothUserAndBusinessRep()
    {
        // ARRANGE
        var user = new User("rep_full_update", "rep_full@biz.com", "6666666666", "business_user", "Initial Address","test");
        await _userRepository.AddAsync(user);

        var businessId = Guid.NewGuid();
        var businessRep = new BusinessRep(businessId, user.Id, "Initial Branch", "Initial Branch Address");
        await _repository.AddAsync(businessRep);

        // ACT - Update user details
        user.Update("updated_rep@biz.com", "7777777777", "Updated Address");
        await _userRepository.UpdateAsync(user);

        // ACT - Update business rep details
        businessRep.UpdateBranch("Updated Branch", "Updated Branch Address");
        await _repository.UpdateAsync(businessRep);

        // ASSERT - Verify both updates
        var updatedUser = await _userRepository.GetByIdAsync(user.Id);
        var updatedBusinessRep = await _repository.GetByIdAsync(businessRep.Id);

        Assert.Multiple(() =>
        {
            // User assertions
            Assert.That(updatedUser, Is.Not.Null);
            Assert.That(updatedUser!.Email, Is.EqualTo("updated_rep@biz.com"));
            Assert.That(updatedUser.Phone, Is.EqualTo("7777777777"));
            Assert.That(updatedUser.Address, Is.EqualTo("Updated Address"));

            // BusinessRep assertions
            Assert.That(updatedBusinessRep, Is.Not.Null);
            Assert.That(updatedBusinessRep!.BranchName, Is.EqualTo("Updated Branch"));
            Assert.That(updatedBusinessRep.BranchAddress, Is.EqualTo("Updated Branch Address"));
            Assert.That(updatedBusinessRep.UserId, Is.EqualTo(user.Id));
        });
    }
}
