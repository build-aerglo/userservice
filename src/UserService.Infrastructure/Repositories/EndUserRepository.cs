using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

public class EndUserRepository: IEndUserRepository
{
    private readonly string _connectionString;

    public EndUserRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("PostgresConnection")!;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);
    

    public async Task<EndUser?> GetByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM end_user_profiles WHERE id = @Id;";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<EndUser>(sql, new { Id = id });
    }

    public async Task AddAsync(EndUser user)
    {
        const string sql = @"
            INSERT INTO end_user_profiles (id, user_id, preferences, bio, social_links, created_at, updated_at)
            VALUES (@Id, @UserId, @Preferences, @Bio, @SocialLinks, @CreatedAt, @UpdatedAt);";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, user);
    }

   
    
}