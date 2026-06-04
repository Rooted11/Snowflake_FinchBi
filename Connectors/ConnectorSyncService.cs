using Microsoft.Extensions.Options;
using SypherBi.Api.Services;

namespace SypherBi.Api.Connectors;

/// <summary>
/// Background worker that periodically runs every <b>enabled</b> connector and
/// upserts the canonical records into Neon. Idle (and cheap) when nothing is
/// configured, so it is always safe to register.
/// </summary>
public class ConnectorSyncService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ConnectorOptions _opt;
    private readonly ILogger<ConnectorSyncService> _logger;

    public ConnectorSyncService(
        IServiceProvider services,
        IOptions<ConnectorOptions> opt,
        ILogger<ConnectorSyncService> logger)
    {
        _services = services;
        _opt      = opt.Value;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(5, _opt.SyncIntervalMinutes));
        var announcedIdle = false;

        while (!ct.IsCancellationRequested)
        {
            using (var scope = _services.CreateScope())
            {
                var sources = scope.ServiceProvider.GetServices<IDonorSource>()
                                   .Where(s => s.Enabled).ToList();

                if (sources.Count == 0)
                {
                    if (!announcedIdle)
                    {
                        _logger.LogInformation("ConnectorSync: no connectors enabled — idle");
                        announcedIdle = true;
                    }
                }
                else
                {
                    announcedIdle = false;
                    var db = scope.ServiceProvider.GetRequiredService<DbService>();
                    foreach (var source in sources)
                    {
                        try
                        {
                            var r = await SyncSourceAsync(source, db, ct);
                            _logger.LogInformation(
                                "ConnectorSync: {Source} upserted {Donors} donors, {Donations} donations",
                                r.Source, r.DonorsUpserted, r.DonationsUpserted);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            _logger.LogError(ex, "ConnectorSync: {Source} failed", source.Name);
                        }
                    }
                }
            }

            try { await Task.Delay(interval, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private static async Task<SyncResult> SyncSourceAsync(IDonorSource source, DbService db, CancellationToken ct)
    {
        const string upsertDonor = @"
            INSERT INTO donors (name, segment_id, max_gift, first_gift, lifecycle)
            VALUES (@Name, @SegmentId, @MaxGift, @FirstGift, @Lifecycle)
            ON CONFLICT (name) DO UPDATE
            SET segment_id = EXCLUDED.segment_id,
                lifecycle  = EXCLUDED.lifecycle,
                max_gift   = GREATEST(donors.max_gift, EXCLUDED.max_gift);";

        // Idempotent without a schema change: skip a gift that already exists.
        // (Production should add a UNIQUE source_ref column and ON CONFLICT on it.)
        const string upsertDonation = @"
            INSERT INTO donations (gift_date, donor_name, campaign_id, channel_id, amount, status)
            SELECT @GiftDate, @DonorName, @CampaignId, @ChannelId, @Amount, @Status
            WHERE NOT EXISTS (
                SELECT 1 FROM donations
                WHERE donor_name = @DonorName AND gift_date = @GiftDate AND amount = @Amount
                  AND campaign_id = @CampaignId AND channel_id = @ChannelId);";

        int donors = 0, donations = 0;

        await foreach (var d in source.FetchDonorsAsync(ct))
            donors += await db.ExecuteAsync(upsertDonor, d);

        await foreach (var g in source.FetchDonationsAsync(ct))
        {
            try { donations += await db.ExecuteAsync(upsertDonation, g); }
            catch { /* unmapped donor/campaign/channel FK — skip this row */ }
        }

        return new SyncResult(source.Name, donors, donations);
    }
}
