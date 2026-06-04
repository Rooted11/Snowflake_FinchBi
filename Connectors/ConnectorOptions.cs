namespace SypherBi.Api.Connectors;

/// <summary>Bound from the "Connectors" section of configuration.</summary>
public class ConnectorOptions
{
    public const string SectionName = "Connectors";

    /// <summary>How often the sync worker runs each enabled source.</summary>
    public int SyncIntervalMinutes { get; set; } = 360;

    public BloomerangOptions Bloomerang { get; set; } = new();
}

public class BloomerangOptions
{
    /// <summary>Turn the connector on. Requires <see cref="ApiKey"/>.</summary>
    public bool Enabled { get; set; }

    /// <summary>Bloomerang API key. Prefer the env var Connectors__Bloomerang__ApiKey.</summary>
    public string ApiKey { get; set; } = "";

    public string BaseUrl { get; set; } = "https://api.bloomerang.co/v2";
}
