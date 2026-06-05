using DashboardService.DTOs;
using HotelOS.Shared.Models;

namespace DashboardService.Services;

public interface IDashboardService
{
    // Real-time Dashboard Data
    Task<HotelOverviewDTO> GetHotelOverviewAsync();
    Task<RoomStatusDashboardDTO> GetRoomStatusDashboardAsync();
    Task<List<ActiveBookingDashboardDTO>> GetActiveBookingsAsync();
    Task<List<OrderDashboardDTO>> GetActiveOrdersAsync();
    Task<List<MaintenanceDashboardDTO>> GetActiveMaintenanceAsync();
    Task<List<StaffWorkloadDTO>> GetStaffWorkloadsAsync();
    
    // Real-time Metrics
    Task<OccupancyMetricsDTO> GetOccupancyMetricsAsync();
    Task<RevenueMetricsDTO> GetRevenueMetricsAsync();
    Task<PerformanceMetricsDTO> GetPerformanceMetricsAsync();
    Task<AlertsDTO> GetSystemAlertsAsync();
    
    // Historical Data
    Task<List<OccupancyTrendDTO>> GetOccupancyTrendsAsync(DateTime startDate, DateTime endDate);
    Task<List<RevenueBreakdownDTO>> GetRevenueBreakdownAsync(DateTime startDate, DateTime endDate);
    Task<List<ServiceMetricsDTO>> GetServiceMetricsAsync(DateTime startDate, DateTime endDate);
    
    // Real-time Event Broadcasting
    Task BroadcastRoomStatusUpdateAsync(int roomId, RoomStatus newStatus);
    Task BroadcastOrderUpdateAsync(int orderId, OrderStatus newStatus);
    Task BroadcastMaintenanceUpdateAsync(int requestId, MaintenanceStatus newStatus);
    Task BroadcastCheckInOutAsync(string eventType, int roomId, string guestName);
    Task BroadcastSystemAlertAsync(SystemAlert alert);
}

public enum DashboardUpdateType
{
    RoomStatusChanged,
    OrderStatusChanged,
    MaintenanceStatusChanged,
    GuestCheckedIn,
    GuestCheckedOut,
    SystemAlert,
    StaffWorkloadChanged,
    MetricsUpdated
}