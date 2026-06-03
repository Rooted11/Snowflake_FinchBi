using Dapper;

namespace FinchBi.Api.Services;

public class DataSimulatorService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DataSimulatorService> _logger;

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

    public DataSimulatorService(IServiceProvider services, ILogger<DataSimulatorService> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("DataSimulator: started");

        while (!ct.IsCancellationRequested)
        {
            // Run once per hour (simulates activity throughout the day)
            var now  = DateTime.UtcNow;
            var next = now.Date.AddHours(now.Hour + 1);
            var wait = next - now;
            await Task.Delay(wait, ct);

            try { await SimulateHourlyActivity(); }
            catch (Exception ex) { _logger.LogError(ex, "DataSimulator error"); }
        }
    }

    private async Task SimulateHourlyActivity()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbService>();
        var rng = new Random();

        var today = DateTime.UtcNow.Date;

        // 1–4 new donations per hour
        int donCount = rng.Next(1, 5);
        for (int i = 0; i < donCount; i++)
        {
            var donor    = DonorNames[rng.Next(DonorNames.Length)];
            var campaign = Campaigns[rng.Next(Campaigns.Length)];
            var channel  = Channels[rng.Next(Channels.Length)];
            var amount   = rng.Next(25, 1800);
            var status   = rng.Next(10) < 9 ? "completed" : "pending";

            await db.ExecuteAsync(@"
                INSERT INTO donations (gift_date, donor_name, campaign_id, channel_id, amount, status)
                VALUES (@date, @donor, @campaign, @channel, @amount, @status)
                ON CONFLICT DO NOTHING",
                new { date = today, donor, campaign = campaign.id, channel = channel.id, amount, status });
        }

        // 2–6 new calls per hour
        int callCount = rng.Next(2, 7);
        for (int i = 0; i < callCount; i++)
        {
            var caller  = CallerNames[rng.Next(CallerNames.Length)];
            var contact = DonorNames[rng.Next(DonorNames.Length)];
            var outcome = Outcomes[rng.Next(Outcomes.Length)];
            var dur     = outcome == "answered" ? rng.Next(60, 600) : rng.Next(5, 40);
            var pledge  = outcome == "answered" && rng.Next(3) > 0 ? rng.Next(25, 1200) : 0;
            var callTime = DateTime.UtcNow.AddMinutes(-rng.Next(0, 59));

            await db.ExecuteAsync(@"
                INSERT INTO calls (call_time, caller_name, contact, duration_sec, outcome, pledge)
                VALUES (@callTime, @caller, @contact, @dur, @outcome, @pledge)",
                new { callTime, caller, contact, dur, outcome, pledge });
        }

        _logger.LogInformation("DataSimulator: inserted {Don} donations, {Call} calls",
            donCount, callCount);
    }
}
