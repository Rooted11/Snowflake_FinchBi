using FinchBi.Api.Models;
using Dapper;

namespace FinchBi.Api.Services;

public class SalesAnalyticsService
{
    private readonly DbService _db;

    public SalesAnalyticsService(DbService db) => _db = db;

    public async Task<RevenueSummary> GetRevenueSummaryAsync(int year)
    {
        const string sql = @"
            SELECT
                COALESCE(SUM(total_amount), 0)            AS ""TotalRevenue"",
                COUNT(DISTINCT order_id)::int             AS ""TotalOrders"",
                COUNT(DISTINCT customer_id)::int          AS ""UniqueCustomers"",
                ROUND(COALESCE(AVG(total_amount), 0)::numeric, 2)  AS ""AvgOrderValue""
            FROM orders
            WHERE EXTRACT(YEAR FROM order_date) = @year
              AND status != 'CANCELLED';";

        return await _db.QueryFirstAsync<RevenueSummary>(sql, new { year })
               ?? new RevenueSummary();
    }

    public async Task<IEnumerable<MonthlyRevenue>> GetMonthlyRevenueAsync(int year)
    {
        const string sql = @"
            SELECT
                EXTRACT(MONTH FROM order_date)::int         AS ""Month"",
                TO_CHAR(order_date, 'Mon')                  AS ""MonthName"",
                COALESCE(SUM(total_amount), 0)              AS ""Revenue"",
                COUNT(DISTINCT order_id)::int               AS ""Orders"",
                ROUND(AVG(total_amount)::numeric, 2)        AS ""AvgOrderValue""
            FROM orders
            WHERE EXTRACT(YEAR FROM order_date) = @year
              AND status != 'CANCELLED'
            GROUP BY EXTRACT(MONTH FROM order_date), TO_CHAR(order_date, 'Mon')
            ORDER BY ""Month"";";

        return await _db.QueryAsync<MonthlyRevenue>(sql, new { year });
    }

    public async Task<IEnumerable<TopProduct>> GetTopProductsAsync(int topN = 10)
    {
        const string sql = @"
            SELECT
                p.product_name                                          AS ""ProductName"",
                p.category                                              AS ""Category"",
                SUM(oi.quantity)::int                                   AS ""UnitsSold"",
                SUM(oi.quantity * oi.unit_price)                        AS ""TotalRevenue"",
                ROUND(SUM(oi.quantity * oi.unit_price)
                    / NULLIF(SUM(SUM(oi.quantity * oi.unit_price)) OVER (), 0) * 100, 2)
                                                                        AS ""RevenuePct""
            FROM order_items oi
            JOIN products p ON p.product_id = oi.product_id
            JOIN orders o   ON o.order_id   = oi.order_id
            WHERE o.status != 'CANCELLED'
            GROUP BY p.product_name, p.category
            ORDER BY ""TotalRevenue"" DESC
            LIMIT @topN;";

        return await _db.QueryAsync<TopProduct>(sql, new { topN });
    }

    public async Task<IEnumerable<CustomerSegment>> GetCustomerSegmentsAsync()
    {
        const string sql = @"
            WITH rfm AS (
                SELECT
                    customer_id,
                    EXTRACT(DAY FROM NOW() - MAX(order_date))::int  AS recency,
                    COUNT(DISTINCT order_id)                         AS frequency,
                    SUM(total_amount)                                AS monetary
                FROM orders
                WHERE status != 'CANCELLED'
                GROUP BY customer_id
            ),
            scored AS (
                SELECT *,
                    NTILE(5) OVER (ORDER BY recency ASC)    AS r_score,
                    NTILE(5) OVER (ORDER BY frequency DESC) AS f_score,
                    NTILE(5) OVER (ORDER BY monetary DESC)  AS m_score
                FROM rfm
            )
            SELECT
                CASE
                    WHEN r_score >= 4 AND f_score >= 4 THEN 'Champions'
                    WHEN r_score >= 3 AND f_score >= 3 THEN 'Loyal Customers'
                    WHEN r_score >= 4 AND f_score < 2  THEN 'New Customers'
                    WHEN r_score < 2  AND f_score >= 3 THEN 'At Risk'
                    WHEN r_score < 2  AND f_score < 2  THEN 'Churned'
                    ELSE 'Potential Loyalists'
                END                                 AS ""Segment"",
                COUNT(*)::int                       AS ""CustomerCount"",
                ROUND(AVG(monetary)::numeric, 2)    AS ""AvgLtv"",
                ROUND(AVG(recency)::numeric, 0)::int AS ""AvgRecencyDays""
            FROM scored
            GROUP BY ""Segment""
            ORDER BY ""AvgLtv"" DESC;";

        return await _db.QueryAsync<CustomerSegment>(sql);
    }

    public async Task<IEnumerable<RegionPerformance>> GetRegionPerformanceAsync()
    {
        const string sql = @"
            SELECT
                c.region                                                    AS ""Region"",
                SUM(o.total_amount)                                         AS ""Revenue"",
                COUNT(DISTINCT o.order_id)::int                             AS ""Orders"",
                COUNT(DISTINCT o.customer_id)::int                          AS ""Customers"",
                ROUND(SUM(o.total_amount)
                    / NULLIF(COUNT(DISTINCT o.customer_id), 0)::numeric, 2) AS ""RevenuePerCustomer""
            FROM orders o
            JOIN customers c ON c.customer_id = o.customer_id
            WHERE o.status != 'CANCELLED'
            GROUP BY c.region
            ORDER BY ""Revenue"" DESC;";

        return await _db.QueryAsync<RegionPerformance>(sql);
    }

    public async Task<IEnumerable<InventoryItem>> GetLowStockAsync(int threshold = 50)
    {
        const string sql = @"
            SELECT
                product_id::text                            AS ""ProductId"",
                product_name                                AS ""ProductName"",
                category                                    AS ""Category"",
                stock_quantity                              AS ""StockQuantity"",
                reorder_point                               AS ""ReorderPoint"",
                unit_cost                                   AS ""UnitCost"",
                (stock_quantity < reorder_point)            AS ""NeedsReorder""
            FROM products
            WHERE stock_quantity <= @threshold
              AND is_active = TRUE
            ORDER BY stock_quantity ASC
            LIMIT 25;";

        return await _db.QueryAsync<InventoryItem>(sql, new { threshold });
    }
}
