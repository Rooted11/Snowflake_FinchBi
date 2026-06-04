using Dapper;

namespace SypherBi.Api.Services;

/// <summary>
/// Keeps the dashboard "live": hourly incremental top-ups, plus a full fresh rebuild
/// every 24h (and once at startup). The rebuild purges <b>only the simulated layer</b>
/// — rows dated on/after <c>Simulator:LiveSinceUtc</c>, which must be strictly after the
/// seed's latest date — so historical seed data is never touched. Runs only on the
/// Postgres/Neon provider (it writes Postgres-dialect SQL).
/// </summary>
public class DataSimulatorService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DataSimulatorService> _logger;
    private readonly bool _enabled;
    private readonly DateTime _liveSince;

    private static readonly string[] DonorNames = {
        "M. Chen","R. Patel","D. Okafor","S. Nakamura","J. Liu","H. Mwangi",
        "L. Garcia","A. Bekele","J. Thompson","P. Singh","C. Foster","I. Petrov",
        "T. Okonkwo","S. Bergmann","R. Farooqi","P. Osei","J. Castellano","M. Dubois",
        "A. Mensah","C. Oduya","D. Fernandez","H. Tremblay","L. Sorensen","M. Khoury",
        "V. Chowdhury","C. Nakata","B. Henderson","R. Espinoza","Y. Ahmed","F. Park",
        "T. Williams","E. Ortega","N. Kowalski","P. Andersen","R. Johansson","V. Nwosu"
    };

    private static readonly string[] CallerNames = {
        "Maya Rodriguez","Aisha Williams","James Park","Priya Desai","Daniel Cohen",
        "Tom Schaefer","Sofia Martinez","Kenji Tanaka","Rachel Brooks","Marcus Lee","Naomi Ferreira"
    };

    private static readonly (string id, string label)[] Campaigns = {
        ("spring","Spring appeal"),("monthly","Monthly giving"),
        ("major","Major gifts"),("eoy","End-of-year"),("capital","Capital campaign")
    };

    private static readonly (string id, string label)[] Channels = {
        ("online","Online"),("phone","Phone"),("mail","Mail"),("event","Event")
    };

    private static readonly string[] Outcomes = { "answered","answered","answered","voicemail","missed" };

    public DataSimulatorService(IServiceProvider services, IConfiguration config, ILogger<DataSimulatorService> logger)
    {
        _services = services;
        _logger   = logger;
        _enabled  = config.GetValue("Simulator:Enabled", true);
        _liveSince = config.GetValue("Simulator:LiveSinceUtc", new DateTime(2026, 6, 4)).Date;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_enabled) { _logger.LogInformation("DataSimulator: disabled via config"); return; }

        // The simulator writes Postgres-dialect SQL; only run it against Neon/Postgres.
        using (var scope = _services.CreateScope())
        {
            var provider = scope.ServiceProvider.GetRequiredService<DbService>().Provider;
            if (provider != "Neon")
            {
                _logger.LogInformation("DataSimulator: provider is {Provider} — simulator idle (Postgres only)", provider);
                return;
            }
        }

        _logger.LogInformation("DataSimulator: started (live layer since {Since:yyyy-MM-dd})", _liveSince);

        // Fresh build on startup so the dashboard always opens on current data.
        await SafeRun(DailyRebuildAsync);
        var lastRebuild = DateTime.UtcNow.Date;

        while (!ct.IsCancellationRequested)
        {
            var now  = DateTime.UtcNow;
            var next = now.Date.AddHours(now.Hour + 1);      // top of the next hour
            try { await Task.Delay(next - now, ct); }
            catch (OperationCanceledException) { break; }

            // Once the UTC day rolls over, rebuild fresh (every 24h).
            if (DateTime.UtcNow.Date > lastRebuild)
            {
                await SafeRun(DailyRebuildAsync);
                lastRebuild = DateTime.UtcNow.Date;
            }

            await SafeRun(SimulateHourlyActivityAsync);
        }
    }

    private async Task SafeRun(Func<DbService, Task> work)
    {
        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DbService>();
            await work(db);
        }
        catch (Exception ex) { _logger.LogError(ex, "DataSimulator error"); }
    }

    // ── Fresh 24h rebuild ─────────────────────────────────────────────────────
    // Purge the simulated layer (only rows >= LiveSinceUtc), then lay down a fresh,
    // fuller "today" of activity. Seed history (< LiveSinceUtc) is untouched.
    private async Task DailyRebuildAsync(DbService db)
    {
        var deletedCalls = await db.ExecuteAsync("DELETE FROM calls WHERE call_time >= @since", new { since = _liveSince });
        var deletedDons  = await db.ExecuteAsync("DELETE FROM donations WHERE gift_date >= @since", new { since = _liveSince });

        var rng   = new Random();
        var today = DateTime.UtcNow.Date;

        int donCount  = rng.Next(25, 46);
        for (int i = 0; i < donCount; i++)
            await InsertDonation(db, rng, today);

        int callCount = rng.Next(35, 61);
        for (int i = 0; i < callCount; i++)
            await InsertCall(db, rng, today.AddHours(9).AddMinutes(rng.Next(0, 540)));   // 9am–6pm today

        _logger.LogInformation(
            "DataSimulator: fresh rebuild — purged {DelDon} donations / {DelCall} calls, seeded {Don} donations / {Call} calls",
            deletedDons, deletedCalls, donCount, callCount);
    }

    // ── Hourly incremental top-up ───────────────────────────────────────────────
    private async Task SimulateHourlyActivityAsync(DbService db)
    {
        var rng   = new Random();
        var today = DateTime.UtcNow.Date;

        int donCount = rng.Next(1, 5);
        for (int i = 0; i < donCount; i++)
            await InsertDonation(db, rng, today);

        int callCount = rng.Next(2, 7);
        for (int i = 0; i < callCount; i++)
            await InsertCall(db, rng, DateTime.UtcNow.AddMinutes(-rng.Next(0, 59)));

        _logger.LogInformation("DataSimulator: hourly +{Don} donations, +{Call} calls", donCount, callCount);
    }

    private static Task InsertDonation(DbService db, Random rng, DateTime date)
    {
        var donor    = DonorNames[rng.Next(DonorNames.Length)];
        var campaign = Campaigns[rng.Next(Campaigns.Length)];
        var channel  = Channels[rng.Next(Channels.Length)];
        var amount   = rng.Next(25, 1800);
        var status   = rng.Next(10) < 9 ? "completed" : "pending";

        return db.ExecuteAsync(@"
            INSERT INTO donations (gift_date, donor_name, campaign_id, channel_id, amount, status)
            VALUES (@date, @donor, @campaign, @channel, @amount, @status)
            ON CONFLICT DO NOTHING",
            new { date, donor, campaign = campaign.id, channel = channel.id, amount, status });
    }

    private static Task InsertCall(DbService db, Random rng, DateTime callTime)
    {
        var caller  = CallerNames[rng.Next(CallerNames.Length)];
        var contact = DonorNames[rng.Next(DonorNames.Length)];
        var outcome = Outcomes[rng.Next(Outcomes.Length)];
        var dur     = outcome == "answered" ? rng.Next(60, 600) : rng.Next(5, 40);
        var pledge  = outcome == "answered" && rng.Next(3) > 0 ? rng.Next(25, 1200) : 0;

        return db.ExecuteAsync(@"
            INSERT INTO calls (call_time, caller_name, contact, duration_sec, outcome, pledge)
            VALUES (@callTime, @caller, @contact, @dur, @outcome, @pledge)",
            new { callTime, caller, contact, dur, outcome, pledge });
    }
}
