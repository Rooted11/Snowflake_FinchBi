namespace SypherBi.Api.Services;

/// <summary>
/// Dialect-specific SQL for every analytics query. One implementation per warehouse
/// (<see cref="PostgresAnalyticsSql"/>, <see cref="SnowflakeAnalyticsSql"/>); the active
/// one is chosen at startup from <c>Database:Provider</c>. <see cref="SypherBiService"/>
/// is dialect-agnostic — it just reads these strings and runs them via Dapper.
/// </summary>
public interface IAnalyticsSql
{
    string Overview { get; }
    string DonationSummary { get; }
    string MonthlyDonations { get; }
    string CampaignBreakdown { get; }
    string ChannelBreakdown { get; }
    string DonationRows { get; }            // param: @limit
    string CallSummary { get; }
    string CallOutcomes { get; }
    string CallerLeaderboard { get; }
    string CallRows { get; }                // param: @limit
    string DonorSegments { get; }
    string DonorLifecycle { get; }
    string DonorRoster { get; }
    string AtRiskDonors { get; }
    string MonthlyTrend { get; }
    string MonthlyCalls { get; }
    string DailyTrend { get; }
    string HourHeatmap { get; }
    string AllDonationsForExport { get; }
    string AllCallsForExport { get; }
}
