using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class BusinessRepRepository : IBusinessRepRepository
{
    private readonly string? _connectionString;
    private readonly IDbConnection? _testConnection;

    public BusinessRepRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public BusinessRepRepository(IDbConnection connection)
    {
        _testConnection = connection;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private bool IsTestMode => _testConnection != null;

    // SQLite needs string params for TEXT uuid columns; Postgres needs Guid for uuid columns.
    private object GuidParam(Guid id) => IsTestMode ? (object)id.ToString() : id;

    private IDbConnection GetConnection() =>
        _testConnection ?? new NpgsqlConnection(_connectionString);

    private static async Task EnsureOpenAsync(IDbConnection connection)
    {
        if (connection.State != ConnectionState.Open)
            await ((System.Data.Common.DbConnection)connection).OpenAsync();
    }

    private async Task DisposeIfOwnedAsync(IDbConnection connection)
    {
        if (!IsTestMode)
            await ((IAsyncDisposable)connection).DisposeAsync();
    }

    public async Task<IEnumerable<BusinessRep>> GetByBusinessIdAsync(Guid businessId)
    {
        const string sql = "SELECT * FROM business_reps WHERE business_id = @BusinessId ORDER BY created_at DESC;";
        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            return await conn.QueryAsync<BusinessRep>(sql, new { BusinessId = GuidParam(businessId) });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task<BusinessRep?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM business_reps WHERE id = @Id;";
        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            return await conn.QueryFirstOrDefaultAsync<BusinessRep>(sql, new { Id = GuidParam(id) });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task<BusinessRep?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM business_reps WHERE user_id = @UserId;";
        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            return await conn.QueryFirstOrDefaultAsync<BusinessRep>(sql, new { UserId = GuidParam(userId) });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task AddAsync(BusinessRep businessRep)
    {
        const string sql = @"
            INSERT INTO business_reps (id, business_id, user_id, branch_name, branch_address, created_at, updated_at)
            VALUES (@Id, @BusinessId, @UserId, @BranchName, @BranchAddress, @CreatedAt, @UpdatedAt);";

        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            await conn.ExecuteAsync(sql, new
            {
                Id            = GuidParam(businessRep.Id),
                BusinessId    = GuidParam(businessRep.BusinessId),
                UserId        = GuidParam(businessRep.UserId),
                businessRep.BranchName,
                businessRep.BranchAddress,
                businessRep.CreatedAt,
                businessRep.UpdatedAt
            });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task UpdateAsync(BusinessRep businessRep)
    {
        const string sql = @"
            UPDATE business_reps
            SET branch_name    = @BranchName,
                branch_address = @BranchAddress,
                updated_at     = @UpdatedAt
            WHERE id = @Id;";

        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            await conn.ExecuteAsync(sql, new
            {
                Id = GuidParam(businessRep.Id),
                businessRep.BranchName,
                businessRep.BranchAddress,
                businessRep.UpdatedAt
            });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM business_reps WHERE id = @Id;";
        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            await conn.ExecuteAsync(sql, new { Id = GuidParam(id) });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task<BusinessRep?> GetParentRepByBusinessIdAsync(Guid businessId)
    {
        const string sql = @"
            SELECT * FROM business_reps
            WHERE business_id = @BusinessId
            ORDER BY created_at ASC
            LIMIT 1;";

        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            return await conn.QueryFirstOrDefaultAsync<BusinessRep>(sql, new { BusinessId = GuidParam(businessId) });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }
}