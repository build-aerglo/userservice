using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class EmailUpdateRequestRepository : IEmailUpdateRequestRepository
{
    private readonly string _connectionString;

    public EmailUpdateRequestRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task AddAsync(EmailUpdateRequest request)
    {
        const string sql = @"
            INSERT INTO email_update_requests (id, business_id, email, reason, created_at)
            VALUES (@Id, @BusinessId, @Email, @Reason, @CreatedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, request);
    }

    public async Task DeleteByBusinessIdAsync(Guid businessId)
    {
        const string sql = "DELETE FROM email_update_requests WHERE business_id = @BusinessId;";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { BusinessId = businessId });
    }
}
