using Microsoft.AspNetCore.Mvc;
using FinchBi.Api.Models;
using FinchBi.Api.Services;

namespace FinchBi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly SalesAnalyticsService _analytics;
    private readonly DbService _db;

    public AnalyticsController(SalesAnalyticsService analytics, DbService db)
    {
        _analytics = analytics;
        _db = db;
    }

    /// <summary>Supabase/Postgres connection health check.</summary>
    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var version = await _db.ExecuteScalarAsync<string>("SELECT version();");
        return Ok(new { status = "ok", postgres = version, utc = DateTime.UtcNow });
    }

    /// <summary>Revenue KPIs for a given year.</summary>
    [HttpGet("revenue/summary")]
    public async Task<IActionResult> GetRevenueSummary([FromQuery] int year = 0)
    {
        year = year == 0 ? 2025 : year;
        var data = await _analytics.GetRevenueSummaryAsync(year);
        return Ok(new ApiResponse<RevenueSummary> { Data = data });
    }

    /// <summary>Month-by-month revenue breakdown.</summary>
    [HttpGet("revenue/monthly")]
    public async Task<IActionResult> GetMonthlyRevenue([FromQuery] int year = 0)
    {
        year = year == 0 ? 2025 : year;
        var data = await _analytics.GetMonthlyRevenueAsync(year);
        return Ok(new ApiResponse<IEnumerable<MonthlyRevenue>> { Data = data });
    }

    /// <summary>Top N products by revenue.</summary>
    [HttpGet("products/top")]
    public async Task<IActionResult> GetTopProducts([FromQuery] int n = 10)
    {
        var data = await _analytics.GetTopProductsAsync(n);
        return Ok(new ApiResponse<IEnumerable<TopProduct>> { Data = data });
    }

    /// <summary>RFM-based customer segmentation.</summary>
    [HttpGet("customers/segments")]
    public async Task<IActionResult> GetCustomerSegments()
    {
        var data = await _analytics.GetCustomerSegmentsAsync();
        return Ok(new ApiResponse<IEnumerable<CustomerSegment>> { Data = data });
    }

    /// <summary>Revenue breakdown by region.</summary>
    [HttpGet("regions")]
    public async Task<IActionResult> GetRegionPerformance()
    {
        var data = await _analytics.GetRegionPerformanceAsync();
        return Ok(new ApiResponse<IEnumerable<RegionPerformance>> { Data = data });
    }

    /// <summary>Products at or below the low-stock threshold.</summary>
    [HttpGet("inventory/low-stock")]
    public async Task<IActionResult> GetLowStock([FromQuery] int threshold = 50)
    {
        var data = await _analytics.GetLowStockAsync(threshold);
        return Ok(new ApiResponse<IEnumerable<InventoryItem>> { Data = data });
    }
}
