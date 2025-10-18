using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;


public class BusinessRepRepository : IBusinessRepRepository
{
    private readonly string _connectionString;

    public BusinessRepRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        
        // Configure Dapper to map snake_case columns to PascalCase properties
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    // Helper method to create database connections
    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<IEnumerable<BusinessRep>> GetByBusinessIdAsync(Guid businessId)
    {
        const string sql = "SELECT * FROM business_reps WHERE business_id = @BusinessId ORDER BY created_at DESC;";
        using var conn = CreateConnection();
        return await conn.QueryAsync<BusinessRep>(sql, new { BusinessId = businessId });
    }

    /// <summary>
    /// Check if a business exists in the database
    /// TODO: Replace with API call to BusinessService when it's running
    /// Example: await _httpClient.GetAsync($"https://business-service/api/businesses/{businessId}/exists")
    /// </summary>
    public async Task<bool> CheckBusinessExistsInDatabase(Guid businessId)
    {
        const string sql = "SELECT COUNT(1) FROM businesses WHERE id = @BusinessId;";
        using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(sql, new { BusinessId = businessId });
        return count > 0;
    }

    public async Task<BusinessRep?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM business_reps WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<BusinessRep>(sql, new { Id = id });
    }

    public async Task<BusinessRep?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM business_reps WHERE user_id = @UserId;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<BusinessRep>(sql, new { UserId = userId });
    }

    public async Task AddAsync(BusinessRep businessRep)
    {
        const string sql = @"
            INSERT INTO business_reps (id, business_id, user_id, branch_name, branch_address, created_at, updated_at)
            VALUES (@Id, @BusinessId, @UserId, @BranchName, @BranchAddress, @CreatedAt, @UpdatedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, businessRep);
    }

    public async Task UpdateAsync(BusinessRep businessRep)
    {
        const string sql = @"
            UPDATE business_reps
            SET branch_name = @BranchName,
                branch_address = @BranchAddress,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, businessRep);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM business_reps WHERE id = @Id;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}