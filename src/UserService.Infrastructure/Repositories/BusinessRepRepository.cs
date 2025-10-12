using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories
{
    public class BusinessRepRepository : IBusinessRepRepository
    {
        private readonly string _connectionString;

        public BusinessRepRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("PostgresConnection")!;
        }

        private NpgsqlConnection CreateConnection() => new(_connectionString);

        public async Task AddAsync(BusinessRep rep)
        {
            const string sql = @"
                INSERT INTO business_reps (id, business_id, user_id, created_at, updated_at)
                VALUES (@Id, @BusinessId, @UserId, @CreatedAt, @UpdatedAt);";

            using var conn = CreateConnection();
            await conn.ExecuteAsync(sql, rep);
        }

        public async Task<IEnumerable<BusinessRep>> GetByBusinessIdAsync(Guid businessId)
        {
            const string sql = "SELECT * FROM business_reps WHERE business_id = @BusinessId;";
            using var conn = CreateConnection();
            return await conn.QueryAsync<BusinessRep>(sql, new { BusinessId = businessId });
        }
    }
}