using Microsoft.AspNetCore.Mvc;
using SypherBi.Api.Models;
using SypherBi.Api.Services;

namespace SypherBi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly SypherBiService _svc;
    private readonly DbService      _db;
    private readonly IConfiguration _config;

    public AnalyticsController(SypherBiService svc, DbService db, IConfiguration config)
    {
        _svc = svc; _db = db; _config = config;
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var versionSql = _db.Provider == "Snowflake" ? "SELECT CURRENT_VERSION()" : "SELECT version();";
        var version = await _db.ExecuteScalarAsync<string>(versionSql);
        return Ok(new { status = "ok", provider = _db.Provider, version, utc = DateTime.UtcNow });
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
        => Ok(new ApiResponse<OverviewSummary> { Data = await _svc.GetOverviewAsync() });

    [HttpGet("donations/summary")]
    public async Task<IActionResult> GetDonationSummary()
        => Ok(new ApiResponse<DonationSummary> { Data = await _svc.GetDonationSummaryAsync() });

    [HttpGet("donations/monthly")]
    public async Task<IActionResult> GetMonthlyDonations()
        => Ok(new ApiResponse<IEnumerable<MonthlyDonations>> { Data = await _svc.GetMonthlyDonationsAsync() });

    [HttpGet("donations/trend")]
    public async Task<IActionResult> GetMonthlyTrend()
        => Ok(new ApiResponse<IEnumerable<MonthlyTrendPoint>> { Data = await _svc.GetMonthlyTrendAsync() });

    [HttpGet("donations/campaigns")]
    public async Task<IActionResult> GetCampaigns()
        => Ok(new ApiResponse<IEnumerable<CampaignBreakdown>> { Data = await _svc.GetCampaignBreakdownAsync() });

    [HttpGet("donations/channels")]
    public async Task<IActionResult> GetChannels()
        => Ok(new ApiResponse<IEnumerable<ChannelBreakdown>> { Data = await _svc.GetChannelBreakdownAsync() });

    [HttpGet("donations/list")]
    public async Task<IActionResult> GetDonationList([FromQuery] int limit = 100)
        => Ok(new ApiResponse<IEnumerable<DonationRow>> { Data = await _svc.GetDonationRowsAsync(limit) });

    [HttpGet("calls/summary")]
    public async Task<IActionResult> GetCallSummary()
        => Ok(new ApiResponse<CallSummary> { Data = await _svc.GetCallSummaryAsync() });

    [HttpGet("calls/outcomes")]
    public async Task<IActionResult> GetCallOutcomes()
        => Ok(new ApiResponse<IEnumerable<CallOutcome>> { Data = await _svc.GetCallOutcomesAsync() });

    [HttpGet("calls/leaderboard")]
    public async Task<IActionResult> GetCallerLeaderboard()
        => Ok(new ApiResponse<IEnumerable<CallerLeaderboard>> { Data = await _svc.GetCallerLeaderboardAsync() });

    [HttpGet("calls/list")]
    public async Task<IActionResult> GetCallList([FromQuery] int limit = 100)
        => Ok(new ApiResponse<IEnumerable<CallRow>> { Data = await _svc.GetCallRowsAsync(limit) });

    [HttpGet("calls/monthly")]
    public async Task<IActionResult> GetMonthlyCalls()
        => Ok(new ApiResponse<IEnumerable<MonthlyCallsPoint>> { Data = await _svc.GetMonthlyCallsAsync() });

    [HttpGet("donors/segments")]
    public async Task<IActionResult> GetDonorSegments()
        => Ok(new ApiResponse<IEnumerable<DonorSegmentSummary>> { Data = await _svc.GetDonorSegmentsAsync() });

    [HttpGet("donors/lifecycle")]
    public async Task<IActionResult> GetDonorLifecycle()
        => Ok(new ApiResponse<IEnumerable<DonorLifecycle>> { Data = await _svc.GetDonorLifecycleAsync() });

    [HttpGet("donors/roster")]
    public async Task<IActionResult> GetDonorRoster()
        => Ok(new ApiResponse<IEnumerable<DonorRosterRow>> { Data = await _svc.GetDonorRosterAsync() });

    [HttpGet("donors/at-risk")]
    public async Task<IActionResult> GetAtRiskDonors()
        => Ok(new ApiResponse<IEnumerable<AtRiskDonor>> { Data = await _svc.GetAtRiskDonorsAsync() });

    // ── Extra endpoints ─────────────────────────────────────────────────────────
    [HttpGet("donations/daily")]
    public async Task<IActionResult> GetDailyTrend()
    {
        var data = await _svc.GetDailyTrendAsync();
        return Ok(new ApiResponse<IEnumerable<DailyTrendPoint>> { Data = data });
    }

    [HttpGet("calls/heatmap")]
    public async Task<IActionResult> GetHourHeatmap()
    {
        var data = await _svc.GetHourHeatmapAsync();
        return Ok(new ApiResponse<IEnumerable<HourHeatmapPoint>> { Data = data });
    }

    [HttpGet("donations/export")]
    public async Task<IActionResult> ExportDonations()
    {
        var data = await _svc.GetAllDonationsForExportAsync();
        var csv  = "Date,Donor,Campaign,Channel,Amount,Status\n" +
                   string.Join("\n", data.Select(d =>
                       $"{d.GiftDate},{d.DonorName},{d.Campaign},{d.Channel},{d.Amount},{d.Status}"));
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "donations.csv");
    }

    [HttpGet("calls/export")]
    public async Task<IActionResult> ExportCalls()
    {
        var data = await _svc.GetAllCallsForExportAsync();
        var csv  = "Time,Caller,Contact,Duration,Outcome,Pledge\n" +
                   string.Join("\n", data.Select(c =>
                       $"{c.CallTime},{c.CallerName},{c.Contact},{c.DurationLabel},{c.Outcome},{c.Pledge}"));
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "calls.csv");
    }
}
