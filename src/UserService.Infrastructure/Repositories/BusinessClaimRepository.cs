using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class BusinessClaimRepository : IBusinessClaimRepository
{
    private readonly string? _connectionString;
    private readonly IDbConnection? _testConnection;

    public BusinessClaimRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public BusinessClaimRepository(IDbConnection connection)
    {
        _testConnection = connection;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    private bool IsTestMode => _testConnection != null;

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

    public async Task<BusinessClaim?> GetByBusinessIdAsync(Guid businessId)
    {
        const string sql = @"
            SELECT id, business_id, name AS business_name, status, expires_at
            FROM business_claim_request
            WHERE business_id = @BusinessId
            ORDER BY created_at DESC
            LIMIT 1;";

        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            return await conn.QueryFirstOrDefaultAsync<BusinessClaim>(sql, new { BusinessId = GuidParam(businessId) });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }
}
