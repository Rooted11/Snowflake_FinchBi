namespace SypherBi.Api.Models;

// Overview
public class OverviewSummary
{
    public decimal TotalRaised         { get; set; }
    public int     DonorCount          { get; set; }
    public int     CallsPlaced         { get; set; }
    public decimal PledgeConversionPct { get; set; }
}

// Donations
public class DonationSummary
{
    public decimal TotalRaised  { get; set; }
    public int     TotalGifts   { get; set; }
    public int     UniqueDonors { get; set; }
    public decimal AvgGiftSize  { get; set; }
}

public class MonthlyDonations
{
    public int     Month     { get; set; }
    public string  MonthName { get; set; } = "";
    public decimal Revenue   { get; set; }
    public int     Gifts     { get; set; }
}

public class CampaignBreakdown
{
    public string  CampaignId { get; set; } = "";
    public string  Label      { get; set; } = "";
    public string  Color      { get; set; } = "";
    public int     Goal       { get; set; }
    public decimal Raised     { get; set; }
    public decimal PctOfGoal  { get; set; }
}

public class ChannelBreakdown
{
    public string  ChannelId  { get; set; } = "";
    public string  Label      { get; set; } = "";
    public string  Color      { get; set; } = "";
    public decimal Revenue    { get; set; }
    public int     Gifts      { get; set; }
    public decimal PctOfTotal { get; set; }
}

public class DonationRow
{
    public string  GiftDate  { get; set; } = "";
    public string  DonorName { get; set; } = "";
    public string  SegmentId { get; set; } = "";
    public string  Campaign  { get; set; } = "";
    public string  Channel   { get; set; } = "";
    public decimal Amount    { get; set; }
    public string  Status    { get; set; } = "";
}

// Calls
public class CallSummary
{
    public int     CallsPlaced    { get; set; }
    public int     CallsConnected { get; set; }
    public decimal ConnectRatePct { get; set; }
    public int     PledgedCalls   { get; set; }
    public decimal PledgeConvPct  { get; set; }
    public decimal AvgDurationSec { get; set; }
}

public class CallOutcome
{
    public string  Outcome { get; set; } = "";
    public int     Count   { get; set; }
    public decimal Pct     { get; set; }
}

public class CallerLeaderboard
{
    public string  Name           { get; set; } = "";
    public string  Role           { get; set; } = "";
    public string  Tenure         { get; set; } = "";
    public int     CallsPlaced    { get; set; }
    public int     Connected      { get; set; }
    public int     Pledges        { get; set; }
    public decimal TotalPledged   { get; set; }
    public decimal ConnectRatePct { get; set; }
    public decimal ConvRatePct    { get; set; }
}

public class CallRow
{
    public string  CallTime      { get; set; } = "";
    public string  CallerName    { get; set; } = "";
    public string  Contact       { get; set; } = "";
    public string  DurationLabel { get; set; } = "";
    public string  Outcome       { get; set; } = "";
    public decimal Pledge        { get; set; }
    public string? NoteText      { get; set; }
}

// Donors
public class DonorSegmentSummary
{
    public string  SegmentId   { get; set; } = "";
    public string  Label       { get; set; } = "";
    public string  Color       { get; set; } = "";
    public int     Donors      { get; set; }
    public decimal TotalRaised { get; set; }
    public decimal AvgGift     { get; set; }
}

public class DonorLifecycle
{
    public string Lifecycle { get; set; } = "";
    public int    Count     { get; set; }
}

public class DonorRosterRow
{
    public string  Name         { get; set; } = "";
    public string  SegmentId    { get; set; } = "";
    public string  SegmentLabel { get; set; } = "";
    public string  FirstGift    { get; set; } = "";
    public string  Lifecycle    { get; set; } = "";
    public int     Gifts        { get; set; }
    public decimal Total        { get; set; }
    public decimal AvgGift      { get; set; }
}

public class AtRiskDonor
{
    public string  Name          { get; set; } = "";
    public string  SegmentLabel  { get; set; } = "";
    public string  FirstGift     { get; set; } = "";
    public decimal LifetimeValue { get; set; }
    public int     GiftCount     { get; set; }
}

// Trends & heatmap (previously returned as dynamic)
public class MonthlyTrendPoint
{
    public int     Month     { get; set; }
    public string  MonthName { get; set; } = "";
    public decimal OneTime   { get; set; }
    public decimal Recurring { get; set; }
    public decimal Pledges   { get; set; }
}

public class MonthlyCallsPoint
{
    public int     Month       { get; set; }
    public string  MonthName   { get; set; } = "";
    public int     Placed      { get; set; }
    public int     Connected   { get; set; }
    public decimal ConnectRate { get; set; }
}

public class DailyTrendPoint
{
    public DateTime Date    { get; set; }
    public decimal  Revenue { get; set; }
    public int      Gifts   { get; set; }
}

public class HourHeatmapPoint
{
    public int     Hour        { get; set; }
    public int     Total       { get; set; }
    public int     Connected   { get; set; }
    public decimal ConnectRate { get; set; }
}

// Shared wrapper
public class ApiResponse<T>
{
    public bool     Success   { get; set; } = true;
    public T?       Data      { get; set; }
    public string?  Error     { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
