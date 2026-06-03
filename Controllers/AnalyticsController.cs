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
    private readonly IConfiguration _config;

    public AnalyticsController(SalesAnalyticsService analytics, DbService db, IConfiguration config)
    {
        _analytics = analytics;
        _db = db;
        _config = config;
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var version = await _db.ExecuteScalarAsync<string>("SELECT version();");
        var connStr = _config.GetConnectionString("Supabase") ?? "NOT FOUND";
        var host = connStr.Split(';').FirstOrDefault(s => s.StartsWith("Host"))?.Split('=').LastOrDefault() ?? "unknown";
        return Ok(new { status = "ok", postgres = version, host, utc = DateTime.UtcNow });
    }

    [HttpGet("debug")]
    public async Task<IActionResult> Debug()
    {
        var count = await _db.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM orders;");
        var delivered = await _db.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM orders WHERE status != 'CANCELLED';");
        var revenue = await _db.ExecuteScalarAsync<decimal>("SELECT COALESCE(SUM(total_amount),0) FROM orders WHERE status != 'CANCELLED';");
        var year2025 = await _db.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM orders WHERE EXTRACT(YEAR FROM order_date) = 2025 AND status != 'CANCELLED';");
        var rev2025 = await _db.ExecuteScalarAsync<decimal>("SELECT COALESCE(SUM(total_amount),0) FROM orders WHERE EXTRACT(YEAR FROM order_date) = 2025 AND status != 'CANCELLED';");
        return Ok(new { totalOrders = count, delivered, totalRevenue = revenue, orders2025 = year2025, revenue2025 = rev2025 });
    }

    [HttpGet("revenue/summary")]
    public async Task<IActionResult> GetRevenueSummary([FromQuery] int year = 0)
    {
        year = year == 0 ? 2025 : year;
        var data = await _analytics.GetRevenueSummaryAsync(year);
        return Ok(new ApiResponse<RevenueSummary> { Data = data });
    }

    [HttpGet("revenue/monthly")]
    public async Task<IActionResult> GetMonthlyRevenue([FromQuery] int year = 0)
    {
        year = year == 0 ? 2025 : year;
        var data = await _analytics.GetMonthlyRevenueAsync(year);
        return Ok(new ApiResponse<IEnumerable<MonthlyRevenue>> { Data = data });
    }

    [HttpGet("products/top")]
    public async Task<IActionResult> GetTopProducts([FromQuery] int n = 10)
    {
        var data = await _analytics.GetTopProductsAsync(n);
        return Ok(new ApiResponse<IEnumerable<TopProduct>> { Data = data });
    }

    [HttpGet("customers/segments")]
    public async Task<IActionResult> GetCustomerSegments()
    {
        var data = await _analytics.GetCustomerSegmentsAsync();
        return Ok(new ApiResponse<IEnumerable<CustomerSegment>> { Data = data });
    }

    [HttpGet("regions")]
    public async Task<IActionResult> GetRegionPerformance()
    {
        var data = await _analytics.GetRegionPerformanceAsync();
        return Ok(new ApiResponse<IEnumerable<RegionPerformance>> { Data = data });
    }

    [HttpGet("inventory/low-stock")]
    public async Task<IActionResult> GetLowStock([FromQuery] int threshold = 50)
    {
        var data = await _analytics.GetLowStockAsync(threshold);
        return Ok(new ApiResponse<IEnumerable<InventoryItem>> { Data = data });
    }
}
