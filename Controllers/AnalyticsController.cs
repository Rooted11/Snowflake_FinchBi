using Microsoft.AspNetCore.Mvc;
using FinchBi.Api.Models;
using FinchBi.Api.Services;

namespace FinchBi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly FinchBiService _svc;
    private readonly DbService      _db;
    private readonly IConfiguration _config;

    public AnalyticsController(FinchBiService svc, DbService db, IConfiguration config)
    {
        _svc    = svc;
        _db     = db;
        _config = config;
    }

    // Health
    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var version = await _db.ExecuteScalarAsync<string>("SELECT version();");
        var connStr = _config.GetConnectionString("Supabase") ?? "NOT FOUND";
        var host    = connStr.Split(';').FirstOrDefault(s => s.StartsWith("Host"))?.Split('=').LastOrDefault() ?? "unknown";
        return Ok(new { status = "ok", postgres = version, host, utc = DateTime.UtcNow });
    }

    // Overview
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var data = await _svc.GetOverviewAsync();
        return Ok(new ApiResponse<OverviewSummary> { Data = data });
    }

    // Donations
    [HttpGet("donations/summary")]
    public async Task<IActionResult> GetDonationSummary()
    {
        var data = await _svc.GetDonationSummaryAsync();
        return Ok(new ApiResponse<DonationSummary> { Data = data });
    }

    [HttpGet("donations/monthly")]
    public async Task<IActionResult> GetMonthlyDonations()
    {
        var data = await _svc.GetMonthlyDonationsAsync();
        return Ok(new ApiResponse<IEnumerable<MonthlyDonations>> { Data = data });
    }

    [HttpGet("donations/campaigns")]
    public async Task<IActionResult> GetCampaigns()
    {
        var data = await _svc.GetCampaignBreakdownAsync();
        return Ok(new ApiResponse<IEnumerable<CampaignBreakdown>> { Data = data });
    }

    [HttpGet("donations/channels")]
    public async Task<IActionResult> GetChannels()
    {
        var data = await _svc.GetChannelBreakdownAsync();
        return Ok(new ApiResponse<IEnumerable<ChannelBreakdown>> { Data = data });
    }

    [HttpGet("donations/list")]
    public async Task<IActionResult> GetDonationList([FromQuery] int limit = 100)
    {
        var data = await _svc.GetDonationRowsAsync(limit);
        return Ok(new ApiResponse<IEnumerable<DonationRow>> { Data = data });
    }

    // Calls
    [HttpGet("calls/summary")]
    public async Task<IActionResult> GetCallSummary()
    {
        var data = await _svc.GetCallSummaryAsync();
        return Ok(new ApiResponse<CallSummary> { Data = data });
    }

    [HttpGet("calls/outcomes")]
    public async Task<IActionResult> GetCallOutcomes()
    {
        var data = await _svc.GetCallOutcomesAsync();
        return Ok(new ApiResponse<IEnumerable<CallOutcome>> { Data = data });
    }

    [HttpGet("calls/leaderboard")]
    public async Task<IActionResult> GetCallerLeaderboard()
    {
        var data = await _svc.GetCallerLeaderboardAsync();
        return Ok(new ApiResponse<IEnumerable<CallerLeaderboard>> { Data = data });
    }

    [HttpGet("calls/list")]
    public async Task<IActionResult> GetCallList([FromQuery] int limit = 100)
    {
        var data = await _svc.GetCallRowsAsync(limit);
        return Ok(new ApiResponse<IEnumerable<CallRow>> { Data = data });
    }

    // Donors
    [HttpGet("donors/segments")]
    public async Task<IActionResult> GetDonorSegments()
    {
        var data = await _svc.GetDonorSegmentsAsync();
        return Ok(new ApiResponse<IEnumerable<DonorSegmentSummary>> { Data = data });
    }

    [HttpGet("donors/lifecycle")]
    public async Task<IActionResult> GetDonorLifecycle()
    {
        var data = await _svc.GetDonorLifecycleAsync();
        return Ok(new ApiResponse<IEnumerable<DonorLifecycle>> { Data = data });
    }

    [HttpGet("donors/roster")]
    public async Task<IActionResult> GetDonorRoster()
    {
        var data = await _svc.GetDonorRosterAsync();
        return Ok(new ApiResponse<IEnumerable<DonorRosterRow>> { Data = data });
    }

    [HttpGet("donors/at-risk")]
    public async Task<IActionResult> GetAtRiskDonors()
    {
        var data = await _svc.GetAtRiskDonorsAsync();
        return Ok(new ApiResponse<IEnumerable<AtRiskDonor>> { Data = data });
    }
}
