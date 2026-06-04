using SypherBi.Api.Models;
using Dapper;

namespace SypherBi.Api.Services;

public class SypherBiService
{
    private readonly DbService _db;
    public SypherBiService(DbService db) => _db = db;

    // ── Overview ──────────────────────────────────────────────────────────────
    public async Task<OverviewSummary> GetOverviewAsync()
    {
        const string sql = @"
            SELECT
                COALESCE((SELECT SUM(amount) FROM donations WHERE status = 'completed'), 0)
                    AS ""TotalRaised"",
                (SELECT COUNT(DISTINCT donor_name) FROM donations WHERE status = 'completed')::int
                    AS ""DonorCount"",
                (SELECT COUNT(*) FROM calls)::int
                    AS ""CallsPlaced"",
                ROUND(
                    COALESCE(
                        (SELECT COUNT(*) FROM calls WHERE pledge > 0)::numeric /
                        NULLIF((SELECT COUNT(*) FROM calls WHERE outcome = 'answered'), 0) * 100
                    , 0), 1)
                    AS ""PledgeConversionPct"";";
        return await _db.QueryFirstAsync<OverviewSummary>(sql) ?? new OverviewSummary();
    }

    // ── Donations ─────────────────────────────────────────────────────────────
    public async Task<DonationSummary> GetDonationSummaryAsync()
    {
        const string sql = @"
            SELECT
                COALESCE(SUM(amount), 0)                    AS ""TotalRaised"",
                COUNT(*)::int                               AS ""TotalGifts"",
                COUNT(DISTINCT donor_name)::int             AS ""UniqueDonors"",
                ROUND(COALESCE(AVG(amount), 0)::numeric, 0) AS ""AvgGiftSize""
            FROM donations
            WHERE status = 'completed';";
        return await _db.QueryFirstAsync<DonationSummary>(sql) ?? new DonationSummary();
    }

    public async Task<IEnumerable<MonthlyDonations>> GetMonthlyDonationsAsync()
    {
        const string sql = @"
            SELECT
                EXTRACT(MONTH FROM gift_date)::int      AS ""Month"",
                TO_CHAR(gift_date, 'Mon')               AS ""MonthName"",
                COALESCE(SUM(amount), 0)                AS ""Revenue"",
                COUNT(*)::int                           AS ""Gifts""
            FROM donations
            WHERE status = 'completed'
            GROUP BY EXTRACT(MONTH FROM gift_date), TO_CHAR(gift_date, 'Mon')
            ORDER BY ""Month"";";
        return await _db.QueryAsync<MonthlyDonations>(sql);
    }

    public async Task<IEnumerable<CampaignBreakdown>> GetCampaignBreakdownAsync()
    {
        const string sql = @"
            SELECT
                c.id                                                        AS ""CampaignId"",
                c.label                                                     AS ""Label"",
                c.color                                                     AS ""Color"",
                c.goal                                                      AS ""Goal"",
                COALESCE(SUM(d.amount), 0)                                  AS ""Raised"",
                ROUND(COALESCE(SUM(d.amount), 0) * 100.0 / c.goal, 1)      AS ""PctOfGoal""
            FROM campaigns c
            LEFT JOIN donations d ON d.campaign_id = c.id AND d.status = 'completed'
            GROUP BY c.id, c.label, c.color, c.goal
            ORDER BY ""Raised"" DESC;";
        return await _db.QueryAsync<CampaignBreakdown>(sql);
    }

    public async Task<IEnumerable<ChannelBreakdown>> GetChannelBreakdownAsync()
    {
        const string sql = @"
            SELECT
                ch.id                                                               AS ""ChannelId"",
                ch.label                                                            AS ""Label"",
                ch.color                                                            AS ""Color"",
                COALESCE(SUM(d.amount), 0)                                          AS ""Revenue"",
                COUNT(d.id)::int                                                    AS ""Gifts"",
                ROUND(COALESCE(SUM(d.amount), 0) * 100.0 /
                    NULLIF(SUM(SUM(d.amount)) OVER (), 0), 1)                       AS ""PctOfTotal""
            FROM channels ch
            LEFT JOIN donations d ON d.channel_id = ch.id AND d.status = 'completed'
            GROUP BY ch.id, ch.label, ch.color
            ORDER BY ""Revenue"" DESC;";
        return await _db.QueryAsync<ChannelBreakdown>(sql);
    }

    public async Task<IEnumerable<DonationRow>> GetDonationRowsAsync(int limit = 100)
    {
        const string sql = @"
            SELECT
                TO_CHAR(d.gift_date, 'DD Mon')  AS ""GiftDate"",
                d.donor_name                    AS ""DonorName"",
                dn.segment_id                   AS ""SegmentId"",
                c.label                         AS ""Campaign"",
                ch.label                        AS ""Channel"",
                d.amount                        AS ""Amount"",
                d.status                        AS ""Status""
            FROM donations d
            JOIN donors    dn ON dn.name = d.donor_name
            JOIN campaigns c  ON c.id  = d.campaign_id
            JOIN channels  ch ON ch.id = d.channel_id
            ORDER BY d.gift_date DESC
            LIMIT @limit;";
        return await _db.QueryAsync<DonationRow>(sql, new { limit });
    }

    // ── Calls ─────────────────────────────────────────────────────────────────
    public async Task<CallSummary> GetCallSummaryAsync()
    {
        const string sql = @"
            SELECT
                COUNT(*)::int                                           AS ""CallsPlaced"",
                COUNT(*) FILTER (WHERE outcome = 'answered')::int       AS ""CallsConnected"",
                ROUND(
                    COUNT(*) FILTER (WHERE outcome = 'answered') * 100.0
                    / NULLIF(COUNT(*), 0), 1)                           AS ""ConnectRatePct"",
                COUNT(*) FILTER (WHERE pledge > 0)::int                 AS ""PledgedCalls"",
                ROUND(
                    COUNT(*) FILTER (WHERE pledge > 0) * 100.0
                    / NULLIF(COUNT(*) FILTER (WHERE outcome = 'answered'), 0), 1)
                                                                        AS ""PledgeConvPct"",
                ROUND(AVG(duration_sec) FILTER (WHERE outcome = 'answered')::numeric, 0)
                                                                        AS ""AvgDurationSec""
            FROM calls;";
        return await _db.QueryFirstAsync<CallSummary>(sql) ?? new CallSummary();
    }

    public async Task<IEnumerable<CallOutcome>> GetCallOutcomesAsync()
    {
        const string sql = @"
            SELECT
                outcome                                         AS ""Outcome"",
                COUNT(*)::int                                   AS ""Count"",
                ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (), 1) AS ""Pct""
            FROM calls
            GROUP BY outcome
            ORDER BY ""Count"" DESC;";
        return await _db.QueryAsync<CallOutcome>(sql);
    }

    public async Task<IEnumerable<CallerLeaderboard>> GetCallerLeaderboardAsync()
    {
        const string sql = @"
            SELECT
                c.name                                                              AS ""Name"",
                c.role                                                              AS ""Role"",
                c.tenure                                                            AS ""Tenure"",
                COUNT(cl.id)::int                                                   AS ""CallsPlaced"",
                COUNT(cl.id) FILTER (WHERE cl.outcome = 'answered')::int            AS ""Connected"",
                COUNT(cl.id) FILTER (WHERE cl.pledge > 0)::int                      AS ""Pledges"",
                COALESCE(SUM(cl.pledge), 0)                                         AS ""TotalPledged"",
                ROUND(
                    COUNT(cl.id) FILTER (WHERE cl.outcome = 'answered') * 100.0
                    / NULLIF(COUNT(cl.id), 0), 1)                                   AS ""ConnectRatePct"",
                ROUND(
                    COUNT(cl.id) FILTER (WHERE cl.pledge > 0) * 100.0
                    / NULLIF(COUNT(cl.id) FILTER (WHERE cl.outcome = 'answered'), 0), 1)
                                                                                    AS ""ConvRatePct""
            FROM callers c
            LEFT JOIN calls cl ON cl.caller_name = c.name
            GROUP BY c.name, c.role, c.tenure
            ORDER BY ""TotalPledged"" DESC;";
        return await _db.QueryAsync<CallerLeaderboard>(sql);
    }

    public async Task<IEnumerable<CallRow>> GetCallRowsAsync(int limit = 100)
    {
        const string sql = @"
            SELECT
                TO_CHAR(call_time, 'DD Mon HH24:MI')    AS ""CallTime"",
                caller_name                             AS ""CallerName"",
                contact                                 AS ""Contact"",
                CASE
                    WHEN duration_sec = 0 THEN '0:00'
                    ELSE FLOOR(duration_sec / 60)::text || ':'
                         || LPAD((duration_sec % 60)::text, 2, '0')
                END                                     AS ""DurationLabel"",
                outcome                                 AS ""Outcome"",
                pledge                                  AS ""Pledge"",
                note_text                               AS ""NoteText""
            FROM calls
            ORDER BY call_time DESC
            LIMIT @limit;";
        return await _db.QueryAsync<CallRow>(sql, new { limit });
    }

    // ── Donors ────────────────────────────────────────────────────────────────
    public async Task<IEnumerable<DonorSegmentSummary>> GetDonorSegmentsAsync()
    {
        const string sql = @"
            SELECT
                s.id                                                AS ""SegmentId"",
                s.label                                             AS ""Label"",
                s.color                                             AS ""Color"",
                COUNT(DISTINCT d.donor_name)::int                   AS ""Donors"",
                COALESCE(SUM(d.amount), 0)                          AS ""TotalRaised"",
                ROUND(COALESCE(AVG(d.amount), 0)::numeric, 0)       AS ""AvgGift""
            FROM segments s
            LEFT JOIN donors dn  ON dn.segment_id = s.id
            LEFT JOIN donations d ON d.donor_name = dn.name AND d.status = 'completed'
            GROUP BY s.id, s.label, s.color
            ORDER BY ""TotalRaised"" DESC;";
        return await _db.QueryAsync<DonorSegmentSummary>(sql);
    }

    public async Task<IEnumerable<DonorLifecycle>> GetDonorLifecycleAsync()
    {
        const string sql = @"
            SELECT lifecycle AS ""Lifecycle"", COUNT(*)::int AS ""Count""
            FROM donors
            GROUP BY lifecycle
            ORDER BY ""Count"" DESC;";
        return await _db.QueryAsync<DonorLifecycle>(sql);
    }

    public async Task<IEnumerable<DonorRosterRow>> GetDonorRosterAsync()
    {
        const string sql = @"
            SELECT
                dn.name                                             AS ""Name"",
                dn.segment_id                                       AS ""SegmentId"",
                s.label                                             AS ""SegmentLabel"",
                dn.first_gift                                       AS ""FirstGift"",
                dn.lifecycle                                        AS ""Lifecycle"",
                COUNT(d.id)::int                                    AS ""Gifts"",
                COALESCE(SUM(d.amount), 0)                          AS ""Total"",
                ROUND(COALESCE(AVG(d.amount), 0)::numeric, 0)       AS ""AvgGift""
            FROM donors dn
            JOIN segments s ON s.id = dn.segment_id
            LEFT JOIN donations d ON d.donor_name = dn.name AND d.status = 'completed'
            GROUP BY dn.name, dn.segment_id, s.label, dn.first_gift, dn.lifecycle
            HAVING COUNT(d.id) > 0
            ORDER BY ""Total"" DESC;";
        return await _db.QueryAsync<DonorRosterRow>(sql);
    }

    public async Task<IEnumerable<AtRiskDonor>> GetAtRiskDonorsAsync()
    {
        const string sql = @"
            SELECT
                dn.name                                             AS ""Name"",
                s.label                                             AS ""SegmentLabel"",
                dn.first_gift                                       AS ""FirstGift"",
                COALESCE(SUM(d.amount), 0)                          AS ""LifetimeValue"",
                COUNT(d.id)::int                                    AS ""GiftCount""
            FROM donors dn
            JOIN segments s ON s.id = dn.segment_id
            LEFT JOIN donations d ON d.donor_name = dn.name AND d.status = 'completed'
            WHERE dn.lifecycle = 'lapsed'
            GROUP BY dn.name, s.label, dn.first_gift
            ORDER BY ""LifetimeValue"" DESC;";
        return await _db.QueryAsync<AtRiskDonor>(sql);
    }

    // ── Monthly trend (one-time vs recurring split + monthly placed/connected) ──
    public async Task<IEnumerable<MonthlyTrendPoint>> GetMonthlyTrendAsync()
    {
        const string sql = @"
            SELECT
                EXTRACT(MONTH FROM gift_date)::int          AS ""Month"",
                TO_CHAR(gift_date, 'Mon')                   AS ""MonthName"",
                COALESCE(SUM(amount) FILTER (WHERE dn.segment_id != 'recurring'), 0) AS ""OneTime"",
                COALESCE(SUM(amount) FILTER (WHERE dn.segment_id  = 'recurring'), 0) AS ""Recurring"",
                COALESCE(SUM(amount) * 0.22, 0)             AS ""Pledges""
            FROM donations d
            JOIN donors dn ON dn.name = d.donor_name
            WHERE d.status = 'completed'
            GROUP BY EXTRACT(MONTH FROM gift_date), TO_CHAR(gift_date, 'Mon')
            ORDER BY ""Month"";";
        return await _db.QueryAsync<MonthlyTrendPoint>(sql);
    }

    public async Task<IEnumerable<MonthlyCallsPoint>> GetMonthlyCallsAsync()
    {
        const string sql = @"
            SELECT
                EXTRACT(MONTH FROM call_time)::int              AS ""Month"",
                TO_CHAR(call_time, 'Mon')                       AS ""MonthName"",
                COUNT(*)::int                                   AS ""Placed"",
                COUNT(*) FILTER (WHERE outcome='answered')::int AS ""Connected"",
                ROUND(COUNT(*) FILTER (WHERE outcome='answered') * 100.0
                    / NULLIF(COUNT(*),0), 1)                    AS ""ConnectRate""
            FROM calls
            GROUP BY EXTRACT(MONTH FROM call_time), TO_CHAR(call_time, 'Mon')
            ORDER BY ""Month"";";
        return await _db.QueryAsync<MonthlyCallsPoint>(sql);
    }

    // ── Daily trend (last 30 days) ─────────────────────────────────────────
    public async Task<IEnumerable<DailyTrendPoint>> GetDailyTrendAsync()
    {
        const string sql = @"
            SELECT
                gift_date                           AS ""Date"",
                COALESCE(SUM(amount), 0)            AS ""Revenue"",
                COUNT(*)::int                       AS ""Gifts""
            FROM donations
            WHERE status = 'completed'
              AND gift_date >= CURRENT_DATE - INTERVAL '30 days'
            GROUP BY gift_date
            ORDER BY gift_date;";
        return await _db.QueryAsync<DailyTrendPoint>(sql);
    }

    // ── Hour heatmap ──────────────────────────────────────────────────────
    public async Task<IEnumerable<HourHeatmapPoint>> GetHourHeatmapAsync()
    {
        const string sql = @"
            SELECT
                EXTRACT(HOUR FROM call_time)::int       AS ""Hour"",
                COUNT(*)::int                           AS ""Total"",
                COUNT(*) FILTER (WHERE outcome='answered')::int AS ""Connected"",
                ROUND(
                    COUNT(*) FILTER (WHERE outcome='answered') * 100.0
                    / NULLIF(COUNT(*), 0), 1)            AS ""ConnectRate""
            FROM calls
            GROUP BY EXTRACT(HOUR FROM call_time)
            ORDER BY ""Hour"";";
        return await _db.QueryAsync<HourHeatmapPoint>(sql);
    }

    // ── CSV export data ───────────────────────────────────────────────────
    public async Task<IEnumerable<DonationRow>> GetAllDonationsForExportAsync()
    {
        const string sql = @"
            SELECT
                TO_CHAR(d.gift_date, 'YYYY-MM-DD') AS ""GiftDate"",
                d.donor_name                        AS ""DonorName"",
                c.label                             AS ""Campaign"",
                ch.label                            AS ""Channel"",
                d.amount                            AS ""Amount"",
                d.status                            AS ""Status""
            FROM donations d
            JOIN campaigns c  ON c.id  = d.campaign_id
            JOIN channels  ch ON ch.id = d.channel_id
            ORDER BY d.gift_date DESC;";
        return await _db.QueryAsync<DonationRow>(sql);
    }

    public async Task<IEnumerable<CallRow>> GetAllCallsForExportAsync()
    {
        const string sql = @"
            SELECT
                TO_CHAR(call_time, 'YYYY-MM-DD HH24:MI') AS ""CallTime"",
                caller_name                               AS ""CallerName"",
                contact                                   AS ""Contact"",
                CASE
                    WHEN duration_sec = 0 THEN '0:00'
                    ELSE FLOOR(duration_sec/60)::text || ':' ||
                         LPAD((duration_sec%60)::text, 2, '0')
                END                                       AS ""DurationLabel"",
                outcome                                   AS ""Outcome"",
                pledge                                    AS ""Pledge"",
                note_text                                 AS ""NoteText""
            FROM calls
            ORDER BY call_time DESC;";
        return await _db.QueryAsync<CallRow>(sql);
    }
}
