using FinchBi.Api.Models;

namespace FinchBi.Api.Services;

public class SalesAnalyticsService
{
    private readonly DbService _db;

    public SalesAnalyticsService(DbService db) => _db = db;

    public async Task<RevenueSummary> GetRevenueSummaryAsync(int year)
    {
        const string sql = @"
            SELECT
                COALESCE(SUM(total_amount), 0)            AS total_revenue,
                COUNT(DISTINCT order_id)                  AS total_orders,
                COUNT(DISTINCT customer_id)               AS unique_customers,
                COALESCE(AVG(total_amount), 0)            AS avg_order_value
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
                EXTRACT(MONTH FROM order_date)::int         AS month,
                TO_CHAR(order_date, 'Mon')                  AS month_name,
                COALESCE(SUM(total_amount), 0)              AS revenue,
                COUNT(DISTINCT order_id)                    AS orders,
                ROUND(AVG(total_amount)::numeric, 2)        AS avg_order_value
            FROM orders
            WHERE EXTRACT(YEAR FROM order_date) = @year
              AND status != 'CANCELLED'
            GROUP BY EXTRACT(MONTH FROM order_date), TO_CHAR(order_date, 'Mon')
            ORDER BY month;";

        return await _db.QueryAsync<MonthlyRevenue>(sql, new { year });
    }

    public async Task<IEnumerable<TopProduct>> GetTopProductsAsync(int topN = 10)
    {
        const string sql = @"
            SELECT
                p.product_name,
                p.category,
                SUM(oi.quantity)                                        AS units_sold,
                SUM(oi.quantity * oi.unit_price)                        AS total_revenue,
                ROUND(SUM(oi.quantity * oi.unit_price)
                    / NULLIF(SUM(SUM(oi.quantity * oi.unit_price)) OVER (), 0) * 100, 2)
                                                                        AS revenue_pct
            FROM order_items oi
            JOIN products p ON p.product_id = oi.product_id
            JOIN orders o   ON o.order_id   = oi.order_id
            WHERE o.status != 'CANCELLED'
            GROUP BY p.product_name, p.category
            ORDER BY total_revenue DESC
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
                END                             AS segment,
                COUNT(*)                        AS customer_count,
                ROUND(AVG(monetary)::numeric,2) AS avg_ltv,
                ROUND(AVG(recency)::numeric,0)::int AS avg_recency_days
            FROM scored
            GROUP BY segment
            ORDER BY avg_ltv DESC;";

        return await _db.QueryAsync<CustomerSegment>(sql);
    }

    public async Task<IEnumerable<RegionPerformance>> GetRegionPerformanceAsync()
    {
        const string sql = @"
            SELECT
                c.region,
                SUM(o.total_amount)                                         AS revenue,
                COUNT(DISTINCT o.order_id)                                  AS orders,
                COUNT(DISTINCT o.customer_id)                               AS customers,
                ROUND(SUM(o.total_amount)
                    / NULLIF(COUNT(DISTINCT o.customer_id), 0)::numeric, 2) AS revenue_per_customer
            FROM orders o
            JOIN customers c ON c.customer_id = o.customer_id
            WHERE o.status != 'CANCELLED'
            GROUP BY c.region
            ORDER BY revenue DESC;";

        return await _db.QueryAsync<RegionPerformance>(sql);
    }

    public async Task<IEnumerable<InventoryItem>> GetLowStockAsync(int threshold = 50)
    {
        const string sql = @"
            SELECT
                product_id,
                product_name,
                category,
                stock_quantity,
                reorder_point,
                unit_cost,
                stock_quantity < reorder_point AS needs_reorder
            FROM products
            WHERE stock_quantity <= @threshold
              AND is_active = TRUE
            ORDER BY stock_quantity ASC
            LIMIT 25;";

        return await _db.QueryAsync<InventoryItem>(sql, new { threshold });
    }
}
