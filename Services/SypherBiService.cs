using SypherBi.Api.Models;

namespace SypherBi.Api.Services;

/// <summary>
/// Analytics read API. Dialect-agnostic: every query string comes from the injected
/// <see cref="IAnalyticsSql"/> (Postgres or Snowflake), executed via <see cref="DbService"/>.
/// </summary>
public class SypherBiService
{
    private readonly DbService _db;
    private readonly IAnalyticsSql _sql;

    public SypherBiService(DbService db, IAnalyticsSql sql)
    {
        _db  = db;
        _sql = sql;
    }

    // ── Overview ────────────────────────────────────────────────────────────────
    public async Task<OverviewSummary> GetOverviewAsync()
        => await _db.QueryFirstAsync<OverviewSummary>(_sql.Overview) ?? new OverviewSummary();

    // ── Donations ─────────────────────────────────────────────────────────────
    public async Task<DonationSummary> GetDonationSummaryAsync()
        => await _db.QueryFirstAsync<DonationSummary>(_sql.DonationSummary) ?? new DonationSummary();

    public Task<IEnumerable<MonthlyDonations>> GetMonthlyDonationsAsync()
        => _db.QueryAsync<MonthlyDonations>(_sql.MonthlyDonations);

    public Task<IEnumerable<CampaignBreakdown>> GetCampaignBreakdownAsync()
        => _db.QueryAsync<CampaignBreakdown>(_sql.CampaignBreakdown);

    public Task<IEnumerable<ChannelBreakdown>> GetChannelBreakdownAsync()
        => _db.QueryAsync<ChannelBreakdown>(_sql.ChannelBreakdown);

    public Task<IEnumerable<DonationRow>> GetDonationRowsAsync(int limit = 100)
        => _db.QueryAsync<DonationRow>(_sql.DonationRows, new { limit });

    // ── Calls ─────────────────────────────────────────────────────────────────
    public async Task<CallSummary> GetCallSummaryAsync()
        => await _db.QueryFirstAsync<CallSummary>(_sql.CallSummary) ?? new CallSummary();

    public Task<IEnumerable<CallOutcome>> GetCallOutcomesAsync()
        => _db.QueryAsync<CallOutcome>(_sql.CallOutcomes);

    public Task<IEnumerable<CallerLeaderboard>> GetCallerLeaderboardAsync()
        => _db.QueryAsync<CallerLeaderboard>(_sql.CallerLeaderboard);

    public Task<IEnumerable<CallRow>> GetCallRowsAsync(int limit = 100)
        => _db.QueryAsync<CallRow>(_sql.CallRows, new { limit });

    // ── Donors ────────────────────────────────────────────────────────────────
    public Task<IEnumerable<DonorSegmentSummary>> GetDonorSegmentsAsync()
        => _db.QueryAsync<DonorSegmentSummary>(_sql.DonorSegments);

    public Task<IEnumerable<DonorLifecycle>> GetDonorLifecycleAsync()
        => _db.QueryAsync<DonorLifecycle>(_sql.DonorLifecycle);

    public Task<IEnumerable<DonorRosterRow>> GetDonorRosterAsync()
        => _db.QueryAsync<DonorRosterRow>(_sql.DonorRoster);

    public Task<IEnumerable<AtRiskDonor>> GetAtRiskDonorsAsync()
        => _db.QueryAsync<AtRiskDonor>(_sql.AtRiskDonors);

    // ── Trends & heatmap ────────────────────────────────────────────────────────
    public Task<IEnumerable<MonthlyTrendPoint>> GetMonthlyTrendAsync()
        => _db.QueryAsync<MonthlyTrendPoint>(_sql.MonthlyTrend);

    public Task<IEnumerable<MonthlyCallsPoint>> GetMonthlyCallsAsync()
        => _db.QueryAsync<MonthlyCallsPoint>(_sql.MonthlyCalls);

    public Task<IEnumerable<DailyTrendPoint>> GetDailyTrendAsync()
        => _db.QueryAsync<DailyTrendPoint>(_sql.DailyTrend);

    public Task<IEnumerable<HourHeatmapPoint>> GetHourHeatmapAsync()
        => _db.QueryAsync<HourHeatmapPoint>(_sql.HourHeatmap);

    // ── CSV export data ───────────────────────────────────────────────────────
    public Task<IEnumerable<DonationRow>> GetAllDonationsForExportAsync()
        => _db.QueryAsync<DonationRow>(_sql.AllDonationsForExport);

    public Task<IEnumerable<CallRow>> GetAllCallsForExportAsync()
        => _db.QueryAsync<CallRow>(_sql.AllCallsForExport);
}
