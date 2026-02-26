using Dapper;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure.Tests.Repositories;

[TestFixture]
public class SupportUserProfileRepositoryTests : InMemoryTestBase
{
    private SupportUserProfileRepository _repository = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _repository = new SupportUserProfileRepository(Connection);
    }

    [Test]
    public async Task AddAsync_ShouldInsertAndRetrieveSupportProfile()
    {
        var userId = await CreateUserAsync(userType: "support_user");
        var profile = new SupportUserProfile(userId);

        await _repository.AddAsync(profile);
        var fetched = await _repository.GetByIdAsync(profile.Id);

        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.UserId, Is.EqualTo(userId));
    }

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnProfile_WhenExists()
    {
        var userId = await CreateUserAsync(userType: "support_user");
        var profile = new SupportUserProfile(userId);
        await _repository.AddAsync(profile);

        var result = await _repository.GetByUserIdAsync(userId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.UserId, Is.EqualTo(userId));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnNull_WhenNotExists()
    {
        var result = await _repository.GetByUserIdAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_ShouldModifyTimestamp()
    {
        var userId = await CreateUserAsync(userType: "support_user");
        var profile = new SupportUserProfile(userId);
        await _repository.AddAsync(profile);

        var originalUpdatedAt = profile.UpdatedAt;
        await Task.Delay(50);

        profile.UpdateTimestamp();
        await _repository.UpdateAsync(profile);

        var updated = await _repository.GetByIdAsync(profile.Id);
        Assert.That(updated!.UpdatedAt, Is.GreaterThan(originalUpdatedAt));
    }

    [Test]
    public async Task UpdateAsync_ShouldNotModifyCreatedAt()
    {
        var userId = await CreateUserAsync(userType: "support_user");
        var profile = new SupportUserProfile(userId);
        await _repository.AddAsync(profile);

        var originalCreatedAt = (await _repository.GetByIdAsync(profile.Id))!.CreatedAt;

        await Task.Delay(50);
        profile.UpdateTimestamp();
        await _repository.UpdateAsync(profile);

        var updated = await _repository.GetByIdAsync(profile.Id);
        Assert.That(updated!.CreatedAt, Is.EqualTo(originalCreatedAt).Within(TimeSpan.FromMilliseconds(10)));
        Assert.That(updated.UpdatedAt, Is.GreaterThan(originalCreatedAt));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveProfile()
    {
        var userId = await CreateUserAsync(userType: "support_user");
        var profile = new SupportUserProfile(userId);
        await _repository.AddAsync(profile);

        await _repository.DeleteAsync(profile.Id);

        var result = await _repository.GetByIdAsync(profile.Id);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DeleteUser_ShouldCascadeDeleteProfile()
    {
        var userId = await CreateUserAsync(userType: "support_user");
        var profile = new SupportUserProfile(userId);
        await _repository.AddAsync(profile);

        // Delete parent user directly — cascade should remove support profile
        await Connection.ExecuteAsync("DELETE FROM users WHERE id = @Id", 
            new { Id = userId.ToString() });

        var result = await _repository.GetByIdAsync(profile.Id);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllProfiles()
    {
        var userId1 = await CreateUserAsync(userType: "support_user");
        var userId2 = await CreateUserAsync(userType: "support_user");

        await _repository.AddAsync(new SupportUserProfile(userId1));
        await _repository.AddAsync(new SupportUserProfile(userId2));

        var results = (await _repository.GetAllAsync()).ToList();

        Assert.That(results.Any(r => r.UserId == userId1), Is.True);
        Assert.That(results.Any(r => r.UserId == userId2), Is.True);
    }

    [Test]
    public async Task UpdateAsync_WithNonExistentId_ShouldNotThrow()
    {
        var profile = new SupportUserProfile(Guid.NewGuid());
        Assert.DoesNotThrowAsync(async () => await _repository.UpdateAsync(profile));
    }
}