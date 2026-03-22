using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class SupportUserProfileRepository : ISupportUserProfileRepository
{
    private readonly string? _connectionString;
    private readonly IDbConnection? _testConnection;

    public SupportUserProfileRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public SupportUserProfileRepository(IDbConnection connection)
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

    public async Task<IEnumerable<SupportUserProfile>> GetAllAsync()
    {
        const string sql = "SELECT * FROM support_user ORDER BY created_at DESC;";
        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            return await conn.QueryAsync<SupportUserProfile>(sql);
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task<SupportUserProfile?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM support_user WHERE id = @Id;";
        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            return await conn.QueryFirstOrDefaultAsync<SupportUserProfile>(sql, new { Id = GuidParam(id) });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task<SupportUserProfile?> GetByUserIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM support_user WHERE user_id = @UserId;";
        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            return await conn.QueryFirstOrDefaultAsync<SupportUserProfile>(sql, new { UserId = GuidParam(userId) });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task AddAsync(SupportUserProfile profile)
    {
        const string sql = @"
            INSERT INTO support_user (id, user_id, created_at, updated_at)
            VALUES (@Id, @UserId, @CreatedAt, @UpdatedAt);";

        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            await conn.ExecuteAsync(sql, new
            {
                Id     = GuidParam(profile.Id),
                UserId = GuidParam(profile.UserId),
                profile.CreatedAt,
                profile.UpdatedAt
            });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task UpdateAsync(SupportUserProfile profile)
    {
        const string sql = "UPDATE support_user SET updated_at = @UpdatedAt WHERE id = @Id;";
        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            await conn.ExecuteAsync(sql, new
            {
                Id = GuidParam(profile.Id),
                profile.UpdatedAt
            });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM support_user WHERE id = @Id;";
        var conn = GetConnection();
        try
        {
            await EnsureOpenAsync(conn);
            await conn.ExecuteAsync(sql, new { Id = GuidParam(id) });
        }
        finally { await DisposeIfOwnedAsync(conn); }
    }
}