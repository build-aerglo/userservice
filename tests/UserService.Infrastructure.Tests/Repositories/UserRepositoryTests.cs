using Dapper;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure.Tests.Repositories;

[TestFixture]
public class UserRepositoryTests : InMemoryTestBase
{
    private UserRepository _repository = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _repository = new UserRepository(Connection);
    }

    [Test]
    public async Task AddAsync_ShouldInsertUser_AndGetById_ShouldReturnUser()
    {
        var userId = await CreateUserAsync(userType: "end_user");

        var fetched = await _repository.GetByIdAsync(userId);

        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Id, Is.EqualTo(userId));
        Assert.That(fetched.UserType, Is.EqualTo("end_user"));
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllInsertedUsers()
    {
        var id1 = await CreateUserAsync();
        var id2 = await CreateUserAsync();

        var results = (await _repository.GetAllAsync()).ToList();

        Assert.That(results.Any(u => u.Id == id1), Is.True);
        Assert.That(results.Any(u => u.Id == id2), Is.True);
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        var userId = await CreateUserAsync();
        var user = await _repository.GetByIdAsync(userId);

        user!.Update("updated@example.com", "9999999999", "New Address");
        await _repository.UpdateAsync(user);

        var updated = await _repository.GetByIdAsync(userId);
        Assert.That(updated!.Email, Is.EqualTo("updated@example.com"));
        Assert.That(updated.Phone, Is.EqualTo("9999999999"));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemoveUser()
    {
        var userId = await CreateUserAsync();
        await _repository.DeleteAsync(userId);

        var result = await _repository.GetByIdAsync(userId);
        Assert.That(result, Is.Null);
    }
}