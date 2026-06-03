namespace FinchBi.Api.Models;

public class RevenueSummary
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int UniqueCustomers { get; set; }
    public decimal AvgOrderValue { get; set; }
}

public class MonthlyRevenue
{
    public int Month { get; set; }
    public string MonthName { get; set; } = "";
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
    public decimal AvgOrderValue { get; set; }
}

public class TopProduct
{
    public string ProductName { get; set; } = "";
    public string Category { get; set; } = "";
    public int UnitsSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenuePct { get; set; }
}

public class CustomerSegment
{
    public string Segment { get; set; } = "";
    public int CustomerCount { get; set; }
    public decimal AvgLtv { get; set; }
    public int AvgRecencyDays { get; set; }
}

public class RegionPerformance
{
    public string Region { get; set; } = "";
    public decimal Revenue { get; set; }
    public int Orders { get; set; }
    public int Customers { get; set; }
    public decimal RevenuePerCustomer { get; set; }
}

public class InventoryItem
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Category { get; set; } = "";
    public int StockQuantity { get; set; }
    public int ReorderPoint { get; set; }
    public decimal UnitCost { get; set; }
    public bool NeedsReorder { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public T? Data { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
