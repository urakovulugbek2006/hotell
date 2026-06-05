using HotelOS.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace DashboardService.DTOs;

// Main Dashboard Overview
public class HotelOverviewDTO
{
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int DirtyRooms { get; set; }
    public int MaintenanceRooms { get; set; }
    public decimal OccupancyRate { get; set; }
    public decimal TodaysRevenue { get; set; }
    public int ActiveGuests { get; set; }
    public int PendingOrders { get; set; }
    public int OpenMaintenanceRequests { get; set; }
    public int ActiveStaff { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

// Room Status Dashboard
public class RoomStatusDashboardDTO
{
    public List<RoomStatusItemDTO> Rooms { get; set; } = new();
    public RoomStatusSummaryDTO Summary { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class RoomStatusItemDTO
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public RoomType Type { get; set; }
    public RoomStatus Status { get; set; }
    public string? CurrentGuestName { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? ExpectedCheckOut { get; set; }
    public DateTime? LastCleaned { get; set; }
    public bool HasActiveOrders { get; set; }
    public bool HasMaintenanceIssues { get; set; }
    public string StatusColor { get; set; } = string.Empty;
    public int? MinutesSinceStatusChange { get; set; }
}

public class RoomStatusSummaryDTO
{
    public int Available { get; set; }
    public int Occupied { get; set; }
    public int Dirty { get; set; }
    public int BeingCleaned { get; set; }
    public int Clean { get; set; }
    public int Maintenance { get; set; }
    public int OutOfOrder { get; set; }
}

// Active Operations
public class ActiveBookingDashboardDTO
{
    public int Id { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public DateTime? ActualCheckIn { get; set; }
    public RoomType RoomType { get; set; }
    public decimal TotalAmount { get; set; }
    public int NightsRemaining { get; set; }
    public bool IsOverdue { get; set; }
    public string StatusBadge { get; set; } = string.Empty;
}

public class OrderDashboardDTO
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime OrderTime { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public string? AssignedStaffName { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan? EstimatedCompletionTime { get; set; }
    public bool IsOverdue { get; set; }
    public string StatusColor { get; set; } = string.Empty;
    public string PriorityLevel { get; set; } = string.Empty;
}

public class MaintenanceDashboardDTO
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; }
    public DateTime ReportedAt { get; set; }
    public string? AssignedTechnicianName { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan? EstimatedResolutionTime { get; set; }
    public bool IsOverdue { get; set; }
    public string PriorityColor { get; set; } = string.Empty;
    public string StatusBadge { get; set; } = string.Empty;
    public int QueuePosition { get; set; }
}

// Staff Workload
public class StaffWorkloadDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public StaffRole Role { get; set; }
    public StaffStatus Status { get; set; }
    public int ActiveTasks { get; set; }
    public int CompletedTasksToday { get; set; }
    public string Department { get; set; } = string.Empty;
    public TimeSpan ShiftDuration { get; set; }
    public string CurrentTask { get; set; } = string.Empty;
    public string WorkloadLevel { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
}

// Metrics DTOs
public class OccupancyMetricsDTO
{
    public decimal CurrentOccupancyRate { get; set; }
    public decimal TargetOccupancyRate { get; set; } = 85.0m;
    public decimal WeeklyAverageOccupancy { get; set; }
    public decimal MonthlyAverageOccupancy { get; set; }
    public int CheckInsToday { get; set; }
    public int CheckOutsToday { get; set; }
    public int ArrivalsExpected { get; set; }
    public int DeparturesExpected { get; set; }
    public List<HourlyOccupancyDTO> HourlyTrends { get; set; } = new();
}

public class HourlyOccupancyDTO
{
    public DateTime Hour { get; set; }
    public decimal OccupancyRate { get; set; }
    public int CheckIns { get; set; }
    public int CheckOuts { get; set; }
}

public class RevenueMetricsDTO
{
    public decimal TodaysRevenue { get; set; }
    public decimal WeeklyRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal TargetDailyRevenue { get; set; }
    public decimal AverageRoomRate { get; set; }
    public decimal RevenuePAR { get; set; } // Revenue Per Available Room
    public decimal RoomServiceRevenue { get; set; }
    public decimal AdditionalServicesRevenue { get; set; }
    public List<RevenueBreakdownDTO> CategoryBreakdown { get; set; } = new();
}

public class RevenueBreakdownDTO
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public string Color { get; set; } = string.Empty;
}

public class PerformanceMetricsDTO
{
    public TimeSpan AverageCheckInTime { get; set; }
    public TimeSpan AverageCheckOutTime { get; set; }
    public TimeSpan AverageRoomServiceTime { get; set; }
    public TimeSpan AverageMaintenanceResponseTime { get; set; }
    public TimeSpan AverageCleaningTime { get; set; }
    public decimal GuestSatisfactionScore { get; set; } = 4.5m;
    public decimal StaffEfficiencyScore { get; set; } = 85.0m;
    public int ServiceRequests { get; set; }
    public int ResolvedRequests { get; set; }
    public decimal ResolutionRate => ServiceRequests > 0 ? (decimal)ResolvedRequests / ServiceRequests * 100 : 0;
}

// Alerts and Notifications
public class AlertsDTO
{
    public List<SystemAlert> CriticalAlerts { get; set; } = new();
    public List<SystemAlert> WarningAlerts { get; set; } = new();
    public List<SystemAlert> InfoAlerts { get; set; } = new();
    public int TotalActiveAlerts { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class SystemAlert
{
    public int Id { get; set; }
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string IconClass { get; set; } = string.Empty;
    public string ColorClass { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool RequiresAction { get; set; }
}

public enum AlertType
{
    RoomStatus,
    Maintenance,
    RoomService,
    CheckInOut,
    SystemHealth,
    StaffWorkload,
    Revenue,
    Security
}

public enum AlertSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3,
    Emergency = 4
}

// Trend Analysis
public class OccupancyTrendDTO
{
    public DateTime Date { get; set; }
    public decimal OccupancyRate { get; set; }
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public decimal Revenue { get; set; }
    public decimal AverageRate { get; set; }
}

public class ServiceMetricsDTO
{
    public DateTime Date { get; set; }
    public int RoomServiceOrders { get; set; }
    public TimeSpan AverageDeliveryTime { get; set; }
    public int MaintenanceRequests { get; set; }
    public TimeSpan AverageResolutionTime { get; set; }
    public decimal CustomerSatisfactionScore { get; set; }
    public int StaffUtilizationRate { get; set; }
}

// Real-time Update DTOs
public class DashboardUpdateDTO
{
    public DashboardUpdateType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public object Data { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? RoomNumber { get; set; }
    public bool RequiresRefresh { get; set; }
}