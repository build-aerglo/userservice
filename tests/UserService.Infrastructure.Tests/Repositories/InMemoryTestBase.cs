using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace UserService.Infrastructure.Tests.Repositories;

public abstract class InMemoryTestBase
{
    protected SqliteConnection Connection = null!;
    
    // Add this class to your test project
    public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override void SetValue(IDbDataParameter parameter, Guid value)
            => parameter.Value = value.ToString();

        public override Guid Parse(object value)
            => Guid.Parse((string)value);
    }

    [OneTimeSetUp]
    public async Task BaseGlobalSetup()
    {
        SqlMapper.AddTypeHandler(new GuidTypeHandler()); 
        Connection = new SqliteConnection("Data Source=:memory:");
        await Connection.OpenAsync();
        await Connection.ExecuteAsync("PRAGMA foreign_keys = ON;");
        await CreateSchemaAsync();
    }

    [OneTimeTearDown]
    public async Task BaseGlobalTearDown()
    {
        await Connection.CloseAsync();
        Connection.Dispose();
    }

    [TearDown]
    public async Task BaseTearDown()
    {
        await Connection.ExecuteAsync(@"
            DELETE FROM point_transactions;
        DELETE FROM user_points;
        DELETE FROM user_badges;
        DELETE FROM review;
        DELETE FROM business_category;
        DELETE FROM business_branches;
        DELETE FROM business_reps;
        DELETE FROM support_user;
        DELETE FROM end_user;
        DELETE FROM business;
        DELETE FROM category;
        DELETE FROM users;");
    }

    private async Task CreateSchemaAsync()
    {
        await Connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS users (
                id          TEXT PRIMARY KEY,
                email       TEXT NOT NULL UNIQUE,
                username    TEXT NOT NULL UNIQUE,
                phone       TEXT,
                password    TEXT,
                user_type   TEXT NOT NULL,
                address     TEXT,
                auth_id     TEXT,
                created_at  TEXT NOT NULL,
                updated_at  TEXT
            );

            CREATE TABLE IF NOT EXISTS end_user (
                id           TEXT PRIMARY KEY,
                user_id      TEXT NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
                social_media TEXT,
                created_at   TEXT NOT NULL,
                updated_at   TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS support_user (
                id          TEXT PRIMARY KEY,
                user_id     TEXT NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
                created_at  TEXT NOT NULL,
                updated_at  TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS business_reps (
                id             TEXT PRIMARY KEY,
                business_id    TEXT NOT NULL,
                user_id        TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                branch_name    TEXT,
                branch_address TEXT,
                created_at     TEXT NOT NULL,
                updated_at     TEXT
            );

            CREATE TABLE IF NOT EXISTS business (
                id                  TEXT PRIMARY KEY,
                name                TEXT NOT NULL,
                logo                TEXT,
                business_citytown   TEXT,
                business_state      TEXT,
                business_address    TEXT,
                is_verified         INTEGER DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS business_branches (
                id               TEXT PRIMARY KEY,
                business_id      TEXT NOT NULL REFERENCES business(id),
                branch_citytown  TEXT,
                branch_state     TEXT
            );

            CREATE TABLE IF NOT EXISTS category (
                id   TEXT PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS business_category (
                business_id TEXT NOT NULL REFERENCES business(id),
                category_id TEXT NOT NULL REFERENCES category(id),
                PRIMARY KEY (business_id, category_id)
            );

            CREATE TABLE IF NOT EXISTS review (
                id                TEXT PRIMARY KEY,
                business_id       TEXT NOT NULL REFERENCES business(id),
                location_id       TEXT REFERENCES business_branches(id),
                reviewer_id       TEXT REFERENCES users(id),
                email             TEXT,
                star_rating       REAL,
                review_body       TEXT,
                photo_urls        TEXT,
                review_as_anon    INTEGER DEFAULT 0,
                is_guest_review   INTEGER DEFAULT 0,
                status            TEXT,
                ip_address        TEXT,
                device_id         TEXT,
                geolocation       TEXT,
                user_agent        TEXT,
                validation_result TEXT,
                validated_at      TEXT,
                created_at        TEXT NOT NULL,
                updated_at        TEXT,
                is_verification_pending INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS user_badges (
                id          TEXT PRIMARY KEY,
                user_id     TEXT NOT NULL REFERENCES users(id),
                badge_type  TEXT NOT NULL,
                location    TEXT,
                category    TEXT,
                earned_at   TEXT NOT NULL,
                is_active   INTEGER DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS user_points (
                user_id         TEXT PRIMARY KEY REFERENCES users(id),
                total_points    REAL DEFAULT 0,
                current_streak  INTEGER DEFAULT 0,
                longest_streak  INTEGER DEFAULT 0,
                last_login_date TEXT,
                updated_at      TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS point_transactions (
                id               TEXT PRIMARY KEY,
                user_id          TEXT NOT NULL REFERENCES users(id),
                points           REAL NOT NULL,
                transaction_type TEXT NOT NULL,
                description      TEXT,
                reference_id     TEXT,
                reference_type   TEXT,
                created_at       TEXT NOT NULL
            );");
    }

    // ========================================================================
    // SHARED HELPERS
    // ========================================================================

    protected async Task<Guid> CreateUserAsync(
        Guid? userId = null,
        string userType = "end_user",
        string? username = null,
        string? email = null)
    {
        var id = userId ?? Guid.NewGuid();
        var u = username ?? $"user_{id:N}";
        var e = email ?? $"test_{id:N}@example.com";

        await Connection.ExecuteAsync(@"
            INSERT INTO users (id, email, username, phone, user_type, address, auth_id, created_at)
            VALUES (@Id, @Email, @Username, @Phone, @UserType, @Address, @AuthId, @CreatedAt)",
            new
            {
                Id = id.ToString(),
                Email = e,
                Username = u,
                Phone = "1234567890",
                UserType = userType,
                Address = "Test Address",
                AuthId = "auth0|test",
                CreatedAt = DateTime.UtcNow.ToString("o")
            });

        return id;
    }

    protected async Task<Guid> CreateBusinessAsync(string city = "Test City", string state = "TS")
    {
        var id = Guid.NewGuid();
        await Connection.ExecuteAsync(@"
            INSERT INTO business (id, name, business_citytown, business_state, is_verified)
            VALUES (@Id, @Name, @City, @State, 1)",
            new { Id = id.ToString(), Name = $"Business_{id:N}", City = city, State = state });
        return id;
    }

    protected async Task<Guid> CreateBranchAsync(Guid businessId, string city, string state)
    {
        var id = Guid.NewGuid();
        await Connection.ExecuteAsync(@"
            INSERT INTO business_branches (id, business_id, branch_citytown, branch_state)
            VALUES (@Id, @BusinessId, @City, @State)",
            new { Id = id.ToString(), BusinessId = businessId.ToString(), City = city, State = state });
        return id;
    }

    protected async Task CreateReviewAsync(
        Guid businessId,
        Guid userId,
        string status = "APPROVED",
        decimal starRating = 5m,
        Guid? locationId = null)
    {
        await Connection.ExecuteAsync(@"
            INSERT INTO review (id, business_id, location_id, reviewer_id, email, star_rating, review_body, status, created_at)
            VALUES (@Id, @BusinessId, @LocationId, @ReviewerId, @Email, @StarRating, @ReviewBody, @Status, @CreatedAt)",
            new
            {
                Id = Guid.NewGuid().ToString(),
                BusinessId = businessId.ToString(),
                LocationId = locationId?.ToString(),
                ReviewerId = userId.ToString(),
                Email = $"test_{userId:N}@example.com",
                StarRating = (double)starRating,
                ReviewBody = "Test review body with sufficient content.",
                Status = status,
                CreatedAt = DateTime.UtcNow.ToString("o")
            });
    }
}