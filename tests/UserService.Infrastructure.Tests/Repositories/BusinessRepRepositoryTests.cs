using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure.Tests.Repositories
{
    [TestFixture]
    public class BusinessRepRepositoryTests
    {
        private BusinessRepRepository _repository = null!;
        private UserRepository _userRepository = null!;
        private IConfiguration _configuration = null!;

        private const string ConnectionString =
            "Host=localhost;Port=5432;Database=reviewapp;Username=prnzdiamond;Password=diamond";

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
            _repository = new BusinessRepRepository(_configuration);
            _userRepository = new UserRepository(_configuration);

            // Ensure tables exist (migrations should have created these)
            await using var conn = new NpgsqlConnection(ConnectionString);
            
            // Verify business_reps table exists
            var tableExists = await conn.ExecuteScalarAsync<bool>(@"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_name = 'business_reps'
                );
            ");

            if (!tableExists)
            {
                throw new Exception("business_reps table does not exist. Run migrations first!");
            }
        }

        [SetUp]
        public async Task Setup()
        {
            // Clean up test data before each test
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.ExecuteAsync("DELETE FROM business_reps;");
            await conn.ExecuteAsync("DELETE FROM users WHERE user_type = 'business_user';");
        }

        [Test]
        public async Task CheckBusinessExistsInDatabase_ShouldReturnTrue_WhenBusinessExists()
        {
            // ARRANGE - Create a test business
            var businessId = await CreateTestBusinessIfNotExists();

            // ACT
            var exists = await _repository.CheckBusinessExistsInDatabase(businessId);

            // ASSERT
            Assert.That(exists, Is.True);
        }

        [Test]
        public async Task CheckBusinessExistsInDatabase_ShouldReturnFalse_WhenBusinessDoesNotExist()
        {
            // ARRANGE - Use a random GUID that doesn't exist
            var nonExistentBusinessId = Guid.NewGuid();

            // ACT
            var exists = await _repository.CheckBusinessExistsInDatabase(nonExistentBusinessId);

            // ASSERT
            Assert.That(exists, Is.False);
        }

        [Test]
        public async Task AddAsync_ShouldInsertBusinessRep()
        {
            // ARRANGE - Create a user first (foreign key requirement)
            var user = new User("test_rep", "test@business.com", "1234567890", "business_user", "123 Test St");
            await _userRepository.AddAsync(user);

            // Create a business ID (assuming business exists from migrations or manual setup)
            var businessId = await CreateTestBusinessIfNotExists();

            var businessRep = new BusinessRep(businessId, user.Id, "Main Branch", "456 Branch Ave");

            // ACT
            await _repository.AddAsync(businessRep);

            // ASSERT
            await using var conn = new NpgsqlConnection(ConnectionString);
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM business_reps WHERE user_id = @UserId;",
                new { UserId = user.Id }
            );
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetByIdAsync_ShouldReturnBusinessRep_WhenExists()
        {
            // ARRANGE
            var user = new User("john_rep", "john@business.com", "1234567890", "business_user", "123 Main St");
            await _userRepository.AddAsync(user);

            var businessId = await CreateTestBusinessIfNotExists();
            var businessRep = new BusinessRep(businessId, user.Id, "Test Branch", "789 Test Ave");
            await _repository.AddAsync(businessRep);

            // ACT
            var result = await _repository.GetByIdAsync(businessRep.Id);

            // ASSERT
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.UserId, Is.EqualTo(user.Id));
            Assert.That(result.BusinessId, Is.EqualTo(businessId));
            Assert.That(result.BranchName, Is.EqualTo("Test Branch"));
        }

        [Test]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            // ACT
            var result = await _repository.GetByIdAsync(Guid.NewGuid());

            // ASSERT
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetByUserIdAsync_ShouldReturnBusinessRep()
        {
            // ARRANGE
            var user = new User("jane_rep", "jane@business.com", "9876543210", "business_user", "456 Elm St");
            await _userRepository.AddAsync(user);

            var businessId = await CreateTestBusinessIfNotExists();
            var businessRep = new BusinessRep(businessId, user.Id, "Branch A", "111 A Street");
            await _repository.AddAsync(businessRep);

            // ACT
            var result = await _repository.GetByUserIdAsync(user.Id);

            // ASSERT
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.UserId, Is.EqualTo(user.Id));
            Assert.That(result.BranchName, Is.EqualTo("Branch A"));
        }

        [Test]
        public async Task GetByBusinessIdAsync_ShouldReturnAllRepsForBusiness()
        {
            // ARRANGE
            var businessId = await CreateTestBusinessIfNotExists();

            var user1 = new User("rep1", "rep1@business.com", "1111111111", "business_user", "Address 1");
            var user2 = new User("rep2", "rep2@business.com", "2222222222", "business_user", "Address 2");
            await _userRepository.AddAsync(user1);
            await _userRepository.AddAsync(user2);

            var rep1 = new BusinessRep(businessId, user1.Id, "Branch 1", "Location 1");
            var rep2 = new BusinessRep(businessId, user2.Id, "Branch 2", "Location 2");
            await _repository.AddAsync(rep1);
            await _repository.AddAsync(rep2);

            // ACT
            var results = (await _repository.GetByBusinessIdAsync(businessId)).ToList();

            // ASSERT
            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.Any(r => r.BranchName == "Branch 1"), Is.True);
            Assert.That(results.Any(r => r.BranchName == "Branch 2"), Is.True);
        }

        [Test]
        public async Task UpdateAsync_ShouldModifyBusinessRep()
        {
            // ARRANGE
            var user = new User("update_rep", "update@business.com", "3333333333", "business_user", "Old Address");
            await _userRepository.AddAsync(user);

            var businessId = await CreateTestBusinessIfNotExists();
            var businessRep = new BusinessRep(businessId, user.Id, "Old Branch", "Old Location");
            await _repository.AddAsync(businessRep);

            // Update branch info
            businessRep.UpdateBranch("New Branch", "New Location");
            
            // ACT
            await _repository.UpdateAsync(businessRep);

            // ASSERT
            var updated = await _repository.GetByIdAsync(businessRep.Id);
            Assert.That(updated!.BranchName, Is.EqualTo("New Branch"));
            Assert.That(updated.BranchAddress, Is.EqualTo("New Location"));
        }

        [Test]
        public async Task DeleteAsync_ShouldRemoveBusinessRep()
        {
            // ARRANGE
            var user = new User("delete_rep", "delete@business.com", "4444444444", "business_user", "Delete Address");
            await _userRepository.AddAsync(user);

            var businessId = await CreateTestBusinessIfNotExists();
            var businessRep = new BusinessRep(businessId, user.Id, "Delete Branch", "Delete Location");
            await _repository.AddAsync(businessRep);

            // ACT
            await _repository.DeleteAsync(businessRep.Id);

            // ASSERT
            var result = await _repository.GetByIdAsync(businessRep.Id);
            Assert.That(result, Is.Null);
        }

        private async Task<Guid> CreateTestBusinessIfNotExists()
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            
            // Check if test business exists
            var existingId = await conn.QueryFirstOrDefaultAsync<Guid?>(
                "SELECT id FROM businesses WHERE business_name = 'Test Business' LIMIT 1;"
            );

            if (existingId.HasValue)
                return existingId.Value;

            // Create test business
            var businessId = Guid.NewGuid();
            await conn.ExecuteAsync(@"
                INSERT INTO businesses (
                    id, business_name, business_address, business_email, 
                    business_phone, sector, verified, created_at, updated_at
                ) VALUES (
                    @Id, 'Test Business', 'Test Address', 'test@business.com',
                    '1234567890', 'Technology', false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                );",
                new { Id = businessId }
            );

            return businessId;
        }
    }
}