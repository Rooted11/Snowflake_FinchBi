using Npgsql;
using System.Data;
using Dapper;

namespace FinchBi.Api.Services;

public class DbService
{
    private readonly string _connectionString;
    private readonly ILogger<DbService> _logger;

    public DbService(IConfiguration config, ILogger<DbService> logger)
    {
        _connectionString = config.GetConnectionString("Supabase")
            ?? throw new InvalidOperationException("Supabase connection string not found.");
        _logger = logger;
    }

    public IDbConnection OpenConnection()
    {
        var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        using var conn = OpenConnection();
        _logger.LogDebug("Query: {Sql}", sql);
        return await conn.QueryAsync<T>(sql, param);
    }

    public async Task<T?> QueryFirstAsync<T>(string sql, object? param = null)
    {
        using var conn = OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        using var conn = OpenConnection();
        return await conn.ExecuteAsync(sql, param);
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null)
    {
        using var conn = OpenConnection();
        return await conn.ExecuteScalarAsync<T>(sql, param);
    }
}
