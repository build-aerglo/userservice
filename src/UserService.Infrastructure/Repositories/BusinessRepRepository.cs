using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

/// <summary>
/// Repository responsible only for persistence operations on Business Representatives.
/// It does NOT query the businesses table — business existence is validated externally via API.
/// </summary>
public class BusinessRepRepository : IBusinessRepRepository
{
    private readonly string _connectionString;

    public BusinessRepRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    /// <summary>
    /// Returns all representatives linked to a specific business.
    /// </summary>
    public async Task<IEnumerable<BusinessRep>> GetByBusinessIdAsync(Guid businessId)
    {
        const string sql = @"
            SELECT * 
            FROM business_reps 
            WHERE business_id = @BusinessId
            ORDER BY created_at DESC;";

        await using var conn = CreateConnection();
        return await conn.QueryAsync<BusinessRep>(sql, new { BusinessId = businessId });
    }

    /// <summary>
    /// Returns a specific representative by its unique ID.
    /// </summary>
    public async Task<BusinessRep?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM business_reps WHERE id = @Id;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<BusinessRep>(sql, new { Id = id });
    }

    /// <summary>
    /// Returns the business representative record associated with a user.
    /// </summary>
    public async Task<BusinessRep?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM business_reps WHERE user_id = @UserId;";
        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<BusinessRep>(sql, new { UserId = userId });
    }

    /// <summary>
    /// Inserts a new business representative into the database.
    /// </summary>
    public async Task AddAsync(BusinessRep businessRep)
    {
        const string sql = @"
            INSERT INTO business_reps 
                (id, business_id, user_id, branch_name, branch_address, created_at, updated_at)
            VALUES 
                (@Id, @BusinessId, @UserId, @BranchName, @BranchAddress, @CreatedAt, @UpdatedAt);";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, businessRep);
    }

    /// <summary>
    /// Updates an existing business representative's branch details.
    /// </summary>
    public async Task UpdateAsync(BusinessRep businessRep)
    {
        const string sql = @"
            UPDATE business_reps
            SET 
                branch_name = @BranchName,
                branch_address = @BranchAddress,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, businessRep);
    }

    /// <summary>
    /// Deletes a business representative record.
    /// </summary>
    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM business_reps WHERE id = @Id;";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { Id = id });
    }
    
    /// <summary>
    /// Gets the parent/first business rep for a business (earliest created_at).
    /// This is the rep who has authority to modify business-level settings (DnD, ReviewsPrivate).
    /// </summary>
    public async Task<BusinessRep?> GetParentRepByBusinessIdAsync(Guid businessId)
    {
        const string sql = @"
            SELECT * 
            FROM business_reps 
            WHERE business_id = @BusinessId
            ORDER BY created_at ASC
            LIMIT 1;";

        await using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<BusinessRep>(sql, new { BusinessId = businessId });
    }
}
