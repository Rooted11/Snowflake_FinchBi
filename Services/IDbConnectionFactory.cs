using System.Data;

namespace SypherBi.Api.Services;

/// <summary>
/// Creates database connections for the configured warehouse. One implementation
/// per provider (Neon/Postgres, Snowflake); the active one is chosen at startup
/// from <c>Database:Provider</c>. This is what makes the analytics API
/// warehouse-pluggable without touching the query layer.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>Human-readable provider name ("Neon", "Snowflake") — surfaced by /health.</summary>
    string Provider { get; }

    /// <summary>A new, unopened connection to the active warehouse.</summary>
    IDbConnection Create();
}
