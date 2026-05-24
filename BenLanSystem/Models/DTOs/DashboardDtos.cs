namespace BenLanSystem.Models.DTOs;

public class DashboardSummaryDto
{
    public int TotalBookings { get; set; }
    public int BookingsThisMonth { get; set; }
    public int ActiveFleet { get; set; }
    public int FleetInMaintenance { get; set; }
    public int BookingsToday { get; set; }
    public decimal BookingsConfirmedPercent { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal RevenueTarget { get; set; } = 150_000m;
    public int PendingBookings { get; set; }
    public int PendingPayments { get; set; }
}

public class RouteLoadDto
{
    public string RouteName { get; set; } = string.Empty;
    public int Trips { get; set; }
    public int Passengers { get; set; }
    public int LoadPercent { get; set; }
    public decimal Revenue { get; set; }
}

public class DashboardAlertDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionLabel { get; set; }
    public string? ActionUrl { get; set; }
    public string Severity { get; set; } = "warning"; // info, warning, danger
}
