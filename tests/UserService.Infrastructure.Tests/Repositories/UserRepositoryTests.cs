using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure.Tests.Repositories
{
    [TestFixture]
    public class UserRepositoryTests
    {
        private UserRepository _repository = null!;
        private IConfiguration _configuration = null!;

        private const string ConnectionString =
            "Host=localhost;Port=5432;Database=voicely;Username=dily;Password=password1";

        [OneTimeSetUp]
        public async Task GlobalSetup()
        {
            // Build configuration manually
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PostgresConnection"] = ConnectionString
                });

            _configuration = builder.Build();
            _repository = new UserRepository(_configuration);

            // Ensure DB + table exist
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS users (
                    id UUID PRIMARY KEY,
                    username TEXT NOT NULL,
                    email TEXT NOT NULL,
                    phone TEXT,
                    user_type TEXT,
                    address TEXT,
                    join_date TIMESTAMP,
                    created_at TIMESTAMP,
                    updated_at TIMESTAMP
                );
            ");
        }

        [SetUp]
        public async Task Setup()
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.ExecuteAsync("DELETE FROM users;");
        }

        [Test]
        public async Task AddAsync_ShouldInsertUser()
        {
            // Arrange
            var user = new User("JohnDoe", "john@example.com", "1234567890", "end", "123 Main St");

            // Act
            await _repository.AddAsync(user);

            // Assert
            await using var conn = new NpgsqlConnection(ConnectionString);
            var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users WHERE username = @Username;", new { user.Username });
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetAllAsync_ShouldReturnUsers()
        {
            // Arrange
            var u1 = new User("JohnDoe", "john@example.com", "1234567890", "end", "123 Main St");
            var u2 = new User("JaneDoe", "jane@example.com", "9876543210", "end", "456 Elm St");
            await _repository.AddAsync(u1);
            await _repository.AddAsync(u2);

            // Act
            var result = (await _repository.GetAllAsync()).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Any(u => u.Username == "JaneDoe"));
        }

        [Test]
        public async Task GetByIdAsync_ShouldReturnUser_WhenExists()
        {
            // Arrange
            var user = new User("JohnDoe", "john@example.com", "1234567890", "end", "123 Main St");
            await _repository.AddAsync(user);

            // Act
            var result = await _repository.GetByIdAsync(user.Id);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Email, Is.EqualTo("john@example.com"));
        }

        [Test]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            // Act
            var result = await _repository.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task UpdateAsync_ShouldModifyUser()
        {
            // Arrange
            var user = new User("JohnDoe", "john@example.com", "1234567890", "end", "123 Main St");
            await _repository.AddAsync(user);

            user.Update("new@example.com", "5555555555", "New Address");
            await _repository.UpdateAsync(user);

            // Assert
            var updated = await _repository.GetByIdAsync(user.Id);
            Assert.That(updated!.Email, Is.EqualTo("new@example.com"));
            Assert.That(updated.Address, Is.EqualTo("New Address"));
        }

        [Test]
        public async Task DeleteAsync_ShouldRemoveUser()
        {
            // Arrange
            var user = new User("JohnDoe", "john@example.com", "1234567890", "end", "123 Main St");
            await _repository.AddAsync(user);

            // Act
            await _repository.DeleteAsync(user.Id);

            // Assert
            var result = await _repository.GetByIdAsync(user.Id);
            Assert.That(result, Is.Null);
        }
    }
}
