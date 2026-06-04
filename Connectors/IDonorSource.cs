namespace SypherBi.Api.Connectors;

/// <summary>
/// A read-only source of donor + donation data (a CRM/DMS connector).
/// Implementations OAuth/authenticate, page through the source API, and yield
/// records already normalised to the canonical model. They never write — the
/// <see cref="ConnectorSyncService"/> owns persistence.
/// </summary>
public interface IDonorSource
{
    /// <summary>Display name, e.g. "Bloomerang".</summary>
    string Name { get; }

    /// <summary>Whether this source is configured (credentials present) and turned on.</summary>
    bool Enabled { get; }

    /// <summary>Stream donors/constituents, normalised, paging as needed.</summary>
    IAsyncEnumerable<CanonicalDonor> FetchDonorsAsync(CancellationToken ct);

    /// <summary>Stream gifts/transactions, normalised, paging as needed.</summary>
    IAsyncEnumerable<CanonicalDonation> FetchDonationsAsync(CancellationToken ct);
}
