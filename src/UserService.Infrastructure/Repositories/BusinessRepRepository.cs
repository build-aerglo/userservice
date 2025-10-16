using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

/// <summary>
/// Implementation of IBusinessRepRepository using Dapper for PostgreSQL
/// Dapper is a micro-ORM that maps SQL results to C# objects
/// </summary>
public class BusinessRepRepository : IBusinessRepRepository
{
    private readonly string _connectionString;

    // Constructor - receives configuration to get database connection string
    public BusinessRepRepository(IConfiguration config)
    {
        // Gets connection string from appsettings.json
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    // Helper method to create database connections
    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<IEnumerable<BusinessRep>> GetByBusinessIdAsync(Guid businessId)
    {
        // SQL query to get all reps for a business
        const string sql = "SELECT * FROM business_reps WHERE business_id = @BusinessId ORDER BY created_at DESC;";
        
        // Create connection, execute query, return results
        using var conn = CreateConnection();
        return await conn.QueryAsync<BusinessRep>(sql, new { BusinessId = businessId });
    }

    public async Task<BusinessRep?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM business_reps WHERE id = @Id;";
        using var conn = CreateConnection();
        
        // QueryFirstOrDefaultAsync returns first result or null
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
        // INSERT statement - adds new row to business_reps table
        const string sql = @"
            INSERT INTO business_reps (id, business_id, user_id, branch_name, branch_address, created_at, updated_at)
            VALUES (@Id, @BusinessId, @UserId, @BranchName, @BranchAddress, @CreatedAt, @UpdatedAt);";

        using var conn = CreateConnection();
        
        // ExecuteAsync runs the query without returning data
        // Dapper automatically maps businessRep properties to @Id, @BusinessId, etc.
        await conn.ExecuteAsync(sql, businessRep);
    }

    public async Task UpdateAsync(BusinessRep businessRep)
    {
        // UPDATE statement - modifies existing row
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
        // DELETE statement - removes row
        const string sql = "DELETE FROM business_reps WHERE id = @Id;";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
}