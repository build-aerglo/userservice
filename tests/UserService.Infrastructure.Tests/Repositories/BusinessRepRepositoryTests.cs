using Dapper;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure.Tests.Repositories;

[TestFixture]
public class BusinessRepRepositoryTests : InMemoryTestBase
{
    private BusinessRepRepository _repository = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _repository = new BusinessRepRepository(Connection);
    }

    [Test]
    public async Task AddAsync_ShouldInsertAndRetrieveBusinessRep()
    {
        var userId = await CreateUserAsync(userType: "business_user");
        var businessId = Guid.NewGuid();
        var rep = new BusinessRep(businessId, userId, "Branch A", "Address A");

        await _repository.AddAsync(rep);
        var fetched = await _repository.GetByIdAsync(rep.Id);

        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.UserId, Is.EqualTo(userId));
        Assert.That(fetched.BusinessId, Is.EqualTo(businessId));
        Assert.That(fetched.BranchName, Is.EqualTo("Branch A"));
    }

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnRep_WhenExists()
    {
        var userId = await CreateUserAsync(userType: "business_user");
        var rep = new BusinessRep(Guid.NewGuid(), userId, "Branch X", "Street X");
        await _repository.AddAsync(rep);

        var result = await _repository.GetByUserIdAsync(userId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.BranchName, Is.EqualTo("Branch X"));
    }

    [Test]
    public async Task UpdateAsync_ShouldModifyBranchInfo()
    {
        var userId = await CreateUserAsync(userType: "business_user");
        var rep = new BusinessRep(Guid.NewGuid(), userId, "Old Branch", "Old Location");
        await _repository.AddAsync(rep);

        rep.UpdateBranch("New Branch", "New Location");
        await _repository.UpdateAsync(rep);

        var updated = await _repository.GetByIdAsync(rep.Id);
        Assert.That(updated!.BranchName, Is.EqualTo("New Branch"));
        Assert.That(updated.BranchAddress, Is.EqualTo("New Location"));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveRep()
    {
        var userId = await CreateUserAsync(userType: "business_user");
        var rep = new BusinessRep(Guid.NewGuid(), userId, "Del Branch", "Del Address");
        await _repository.AddAsync(rep);

        await _repository.DeleteAsync(rep.Id);

        var result = await _repository.GetByIdAsync(rep.Id);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByBusinessIdAsync_ShouldReturnAllRepsForBusiness()
    {
        var businessId = Guid.NewGuid();
        var userId1 = await CreateUserAsync(userType: "business_user");
        var userId2 = await CreateUserAsync(userType: "business_user");

        await _repository.AddAsync(new BusinessRep(businessId, userId1, "Branch 1", "Location 1"));
        await _repository.AddAsync(new BusinessRep(businessId, userId2, "Branch 2", "Location 2"));

        var results = (await _repository.GetByBusinessIdAsync(businessId)).ToList();

        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results.Any(r => r.BranchName == "Branch 1"), Is.True);
        Assert.That(results.Any(r => r.BranchName == "Branch 2"), Is.True);
    }

    [Test]
    public async Task GetParentRepByBusinessIdAsync_ShouldReturnEarliestRep()
    {
        var businessId = Guid.NewGuid();
        var userId1 = await CreateUserAsync(userType: "business_user");
        var userId2 = await CreateUserAsync(userType: "business_user");

        // Insert first rep with earlier timestamp
        var parentRep = new BusinessRep(businessId, userId1, "Parent Branch", "Parent Location");
        await Connection.ExecuteAsync(@"
            INSERT INTO business_reps (id, business_id, user_id, branch_name, branch_address, created_at)
            VALUES (@Id, @BusinessId, @UserId, @BranchName, @BranchAddress, @CreatedAt)",
            new
            {
                Id = parentRep.Id.ToString(),
                BusinessId = businessId.ToString(),
                UserId = userId1.ToString(),
                parentRep.BranchName,
                parentRep.BranchAddress,
                CreatedAt = DateTime.UtcNow.AddSeconds(-10).ToString("o")
            });

        var laterRep = new BusinessRep(businessId, userId2, "Child Branch", "Child Location");
        await Connection.ExecuteAsync(@"
            INSERT INTO business_reps (id, business_id, user_id, branch_name, branch_address, created_at)
            VALUES (@Id, @BusinessId, @UserId, @BranchName, @BranchAddress, @CreatedAt)",
            new
            {
                Id = laterRep.Id.ToString(),
                BusinessId = businessId.ToString(),
                UserId = userId2.ToString(),
                laterRep.BranchName,
                laterRep.BranchAddress,
                CreatedAt = DateTime.UtcNow.ToString("o")
            });

        var result = await _repository.GetParentRepByBusinessIdAsync(businessId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.UserId, Is.EqualTo(userId1));
        Assert.That(result.BranchName, Is.EqualTo("Parent Branch"));
    }

    [Test]
    public async Task GetParentRepByBusinessIdAsync_ShouldReturnNull_WhenNoRepsExist()
    {
        var result = await _repository.GetParentRepByBusinessIdAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }
}