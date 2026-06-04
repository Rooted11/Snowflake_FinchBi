namespace SypherBi.Api.Services;

/// <summary>
/// Snowflake dialect of the analytics queries. Differences vs Postgres handled here:
/// <list type="bullet">
///   <item><c>COUNT(*) FILTER (WHERE …)</c> → <c>COUNT(CASE WHEN … THEN 1 END)</c></item>
///   <item><c>::text</c> → <c>::varchar</c>; date math via <c>DATEADD</c></item>
///   <item><c>TO_CHAR(d,'Mon')</c> → <c>'MON'</c> (Snowflake emits upper-case month abbrev.)</item>
/// </list>
/// Active when <c>Database:Provider = "Snowflake"</c>. Best-effort starting point —
/// validate against a live account (esp. <c>LIMIT</c> parameter binding) before relying on it.
/// </summary>
public class SnowflakeAnalyticsSql : IAnalyticsSql
{
    public string Overview => @"
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

    public string DonationSummary => @"
        SELECT
            COALESCE(SUM(amount), 0)                    AS ""TotalRaised"",
            COUNT(*)::int                               AS ""TotalGifts"",
            COUNT(DISTINCT donor_name)::int             AS ""UniqueDonors"",
            ROUND(COALESCE(AVG(amount), 0)::numeric, 0) AS ""AvgGiftSize""
        FROM donations
        WHERE status = 'completed';";

    public string MonthlyDonations => @"
        SELECT
            EXTRACT(MONTH FROM gift_date)::int      AS ""Month"",
            TO_CHAR(gift_date, 'MON')               AS ""MonthName"",
            COALESCE(SUM(amount), 0)                AS ""Revenue"",
            COUNT(*)::int                           AS ""Gifts""
        FROM donations
        WHERE status = 'completed'
        GROUP BY EXTRACT(MONTH FROM gift_date), TO_CHAR(gift_date, 'MON')
        ORDER BY ""Month"";";

    public string CampaignBreakdown => @"
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

    public string ChannelBreakdown => @"
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

    public string DonationRows => @"
        SELECT
            TO_CHAR(d.gift_date, 'DD MON') AS ""GiftDate"",
            d.donor_name                   AS ""DonorName"",
            dn.segment_id                  AS ""SegmentId"",
            c.label                        AS ""Campaign"",
            ch.label                       AS ""Channel"",
            d.amount                       AS ""Amount"",
            d.status                       AS ""Status""
        FROM donations d
        JOIN donors    dn ON dn.name = d.donor_name
        JOIN campaigns c  ON c.id  = d.campaign_id
        JOIN channels  ch ON ch.id = d.channel_id
        ORDER BY d.gift_date DESC
        LIMIT @limit;";

    public string CallSummary => @"
        SELECT
            COUNT(*)::int                                                   AS ""CallsPlaced"",
            COUNT(CASE WHEN outcome = 'answered' THEN 1 END)::int           AS ""CallsConnected"",
            ROUND(
                COUNT(CASE WHEN outcome = 'answered' THEN 1 END) * 100.0
                / NULLIF(COUNT(*), 0), 1)                                   AS ""ConnectRatePct"",
            COUNT(CASE WHEN pledge > 0 THEN 1 END)::int                     AS ""PledgedCalls"",
            ROUND(
                COUNT(CASE WHEN pledge > 0 THEN 1 END) * 100.0
                / NULLIF(COUNT(CASE WHEN outcome = 'answered' THEN 1 END), 0), 1)
                                                                            AS ""PledgeConvPct"",
            ROUND(AVG(CASE WHEN outcome = 'answered' THEN duration_sec END)::numeric, 0)
                                                                            AS ""AvgDurationSec""
        FROM calls;";

    public string CallOutcomes => @"
        SELECT
            outcome                                            AS ""Outcome"",
            COUNT(*)::int                                      AS ""Count"",
            ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (), 1) AS ""Pct""
        FROM calls
        GROUP BY outcome
        ORDER BY ""Count"" DESC;";

    public string CallerLeaderboard => @"
        SELECT
            c.name                                                              AS ""Name"",
            c.role                                                              AS ""Role"",
            c.tenure                                                            AS ""Tenure"",
            COUNT(cl.id)::int                                                   AS ""CallsPlaced"",
            COUNT(CASE WHEN cl.outcome = 'answered' THEN 1 END)::int            AS ""Connected"",
            COUNT(CASE WHEN cl.pledge > 0 THEN 1 END)::int                      AS ""Pledges"",
            COALESCE(SUM(cl.pledge), 0)                                         AS ""TotalPledged"",
            ROUND(
                COUNT(CASE WHEN cl.outcome = 'answered' THEN 1 END) * 100.0
                / NULLIF(COUNT(cl.id), 0), 1)                                   AS ""ConnectRatePct"",
            ROUND(
                COUNT(CASE WHEN cl.pledge > 0 THEN 1 END) * 100.0
                / NULLIF(COUNT(CASE WHEN cl.outcome = 'answered' THEN 1 END), 0), 1)
                                                                                AS ""ConvRatePct""
        FROM callers c
        LEFT JOIN calls cl ON cl.caller_name = c.name
        GROUP BY c.name, c.role, c.tenure
        ORDER BY ""TotalPledged"" DESC;";

    public string CallRows => @"
        SELECT
            TO_CHAR(call_time, 'DD MON HH24:MI')    AS ""CallTime"",
            caller_name                             AS ""CallerName"",
            contact                                 AS ""Contact"",
            CASE
                WHEN duration_sec = 0 THEN '0:00'
                ELSE FLOOR(duration_sec / 60)::varchar || ':'
                     || LPAD((duration_sec % 60)::varchar, 2, '0')
            END                                     AS ""DurationLabel"",
            outcome                                 AS ""Outcome"",
            pledge                                  AS ""Pledge"",
            note_text                               AS ""NoteText""
        FROM calls
        ORDER BY call_time DESC
        LIMIT @limit;";

    public string DonorSegments => @"
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

    public string DonorLifecycle => @"
        SELECT lifecycle AS ""Lifecycle"", COUNT(*)::int AS ""Count""
        FROM donors
        GROUP BY lifecycle
        ORDER BY ""Count"" DESC;";

    public string DonorRoster => @"
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

    public string AtRiskDonors => @"
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

    public string MonthlyTrend => @"
        SELECT
            EXTRACT(MONTH FROM gift_date)::int          AS ""Month"",
            TO_CHAR(gift_date, 'MON')                   AS ""MonthName"",
            COALESCE(SUM(CASE WHEN dn.segment_id != 'recurring' THEN amount END), 0) AS ""OneTime"",
            COALESCE(SUM(CASE WHEN dn.segment_id  = 'recurring' THEN amount END), 0) AS ""Recurring"",
            COALESCE(SUM(amount) * 0.22, 0)             AS ""Pledges""
        FROM donations d
        JOIN donors dn ON dn.name = d.donor_name
        WHERE d.status = 'completed'
        GROUP BY EXTRACT(MONTH FROM gift_date), TO_CHAR(gift_date, 'MON')
        ORDER BY ""Month"";";

    public string MonthlyCalls => @"
        SELECT
            EXTRACT(MONTH FROM call_time)::int                  AS ""Month"",
            TO_CHAR(call_time, 'MON')                           AS ""MonthName"",
            COUNT(*)::int                                       AS ""Placed"",
            COUNT(CASE WHEN outcome='answered' THEN 1 END)::int AS ""Connected"",
            ROUND(COUNT(CASE WHEN outcome='answered' THEN 1 END) * 100.0
                / NULLIF(COUNT(*),0), 1)                        AS ""ConnectRate""
        FROM calls
        GROUP BY EXTRACT(MONTH FROM call_time), TO_CHAR(call_time, 'MON')
        ORDER BY ""Month"";";

    public string DailyTrend => @"
        SELECT
            gift_date                           AS ""Date"",
            COALESCE(SUM(amount), 0)            AS ""Revenue"",
            COUNT(*)::int                       AS ""Gifts""
        FROM donations
        WHERE status = 'completed'
          AND gift_date >= DATEADD(day, -30, CURRENT_DATE())
        GROUP BY gift_date
        ORDER BY gift_date;";

    public string HourHeatmap => @"
        SELECT
            EXTRACT(HOUR FROM call_time)::int                   AS ""Hour"",
            COUNT(*)::int                                       AS ""Total"",
            COUNT(CASE WHEN outcome='answered' THEN 1 END)::int AS ""Connected"",
            ROUND(
                COUNT(CASE WHEN outcome='answered' THEN 1 END) * 100.0
                / NULLIF(COUNT(*), 0), 1)                       AS ""ConnectRate""
        FROM calls
        GROUP BY EXTRACT(HOUR FROM call_time)
        ORDER BY ""Hour"";";

    public string AllDonationsForExport => @"
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

    public string AllCallsForExport => @"
        SELECT
            TO_CHAR(call_time, 'YYYY-MM-DD HH24:MI') AS ""CallTime"",
            caller_name                               AS ""CallerName"",
            contact                                   AS ""Contact"",
            CASE
                WHEN duration_sec = 0 THEN '0:00'
                ELSE FLOOR(duration_sec/60)::varchar || ':' ||
                     LPAD((duration_sec%60)::varchar, 2, '0')
            END                                       AS ""DurationLabel"",
            outcome                                   AS ""Outcome"",
            pledge                                    AS ""Pledge"",
            note_text                                 AS ""NoteText""
        FROM calls
        ORDER BY call_time DESC;";
}
