namespace SypherBi.Api.Connectors;

// ─────────────────────────────────────────────────────────────────────────────
//  Canonical model
//  Every external CRM/DMS connector normalises its records into these shapes
//  before they are upserted into the warehouse (Neon). This is the contract that
//  lets one dashboard work across Bloomerang, Salesforce NPSP, Blackbaud, etc.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>A donor/constituent, normalised to Sypher's segment + lifecycle model.</summary>
public record CanonicalDonor(
    string Name,
    string SegmentId,    // 'major' | 'mid' | 'grass' | 'recurring'
    string Lifecycle,    // 'active' | 'new' | 'lapsed' | 'reactivated'
    string FirstGift,    // 'Mon YYYY'
    int    MaxGift);

/// <summary>A single gift/transaction, normalised to Sypher's donation model.</summary>
public record CanonicalDonation(
    string   SourceRef,   // stable id from the source system (used for idempotency)
    DateTime GiftDate,
    string   DonorName,
    string   CampaignId,  // mapped to campaigns(id)
    string   ChannelId,   // mapped to channels(id)
    decimal  Amount,
    string   Status);     // 'completed' | 'pending' | 'failed'

/// <summary>Outcome of a single connector sync run, surfaced in logs/telemetry.</summary>
public record SyncResult(string Source, int DonorsUpserted, int DonationsUpserted)
{
    public static SyncResult Empty(string source) => new(source, 0, 0);
}
