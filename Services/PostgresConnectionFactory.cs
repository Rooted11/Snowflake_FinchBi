using System.Data;
using Npgsql;

namespace SypherBi.Api.Services;

/// <summary>Neon / Postgres connection factory (the default provider).</summary>
public class PostgresConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public PostgresConnectionFactory(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Neon")
            ?? throw new InvalidOperationException("Neon connection string not found.");
    }

    public string Provider => "Neon";

    public IDbConnection Create() => new NpgsqlConnection(_connectionString);
}
