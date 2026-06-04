using System.Data;
using Snowflake.Data.Client;

namespace SypherBi.Api.Services;

/// <summary>
/// Snowflake connection factory. Connection string uses Snowflake.Data keywords, e.g.
/// <c>account=…;user=…;password=…;db=SYPHER_BI;schema=PUBLIC;warehouse=COMPUTE_WH;role=SYSADMIN</c>.
/// Active when <c>Database:Provider = "Snowflake"</c>.
/// </summary>
public class SnowflakeConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SnowflakeConnectionFactory(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("Snowflake")
            ?? throw new InvalidOperationException("Snowflake connection string not found.");
    }

    public string Provider => "Snowflake";

    public IDbConnection Create() => new SnowflakeDbConnection { ConnectionString = _connectionString };
}
