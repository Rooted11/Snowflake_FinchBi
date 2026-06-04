using System.Data;
using Dapper;

namespace SypherBi.Api.Services;

/// <summary>
/// Thin Dapper helper over whichever warehouse <see cref="IDbConnectionFactory"/> is
/// registered. Provider-agnostic: the SQL dialect comes from <c>IAnalyticsSql</c>.
/// </summary>
public class DbService
{
    private readonly IDbConnectionFactory _factory;
    private readonly ILogger<DbService> _logger;

    public DbService(IDbConnectionFactory factory, ILogger<DbService> logger)
    {
        _factory = factory;
        _logger  = logger;
    }

    /// <summary>The active warehouse provider ("Neon" | "Snowflake").</summary>
    public string Provider => _factory.Provider;

    public IDbConnection OpenConnection()
    {
        var conn = _factory.Create();
        conn.Open();
        return conn;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        using var conn = OpenConnection();
        _logger.LogDebug("Query ({Provider}): {Sql}", _factory.Provider, sql);
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
