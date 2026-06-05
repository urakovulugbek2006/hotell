using DashboardService.DTOs;
using DashboardService.Services;
using Microsoft.AspNetCore.Mvc;

namespace DashboardService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetHotelOverview()
    {
        try
        {
            var overview = await _dashboardService.GetHotelOverviewAsync();
            return Ok(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel overview");
            return StatusCode(500, new { error = "An error occurred while retrieving hotel overview" });
        }
    }

    [HttpGet("rooms/status")]
    public async Task<IActionResult> GetRoomStatus()
    {
        try
        {
            var roomStatus = await _dashboardService.GetRoomStatusDashboardAsync();
            return Ok(roomStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room status");
            return StatusCode(500, new { error = "An error occurred while retrieving room status" });
        }
    }

    [HttpGet("bookings/active")]
    public async Task<IActionResult> GetActiveBookings()
    {
        try
        {
            var bookings = await _dashboardService.GetActiveBookingsAsync();
            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active bookings");
            return StatusCode(500, new { error = "An error occurred while retrieving active bookings" });
        }
    }

    [HttpGet("orders/active")]
    public async Task<IActionResult> GetActiveOrders()
    {
        try
        {
            var orders = await _dashboardService.GetActiveOrdersAsync();
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active orders");
            return StatusCode(500, new { error = "An error occurred while retrieving active orders" });
        }
    }

    [HttpGet("maintenance/active")]
    public async Task<IActionResult> GetActiveMaintenance()
    {
        try
        {
            var maintenance = await _dashboardService.GetActiveMaintenanceAsync();
            return Ok(maintenance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active maintenance");
            return StatusCode(500, new { error = "An error occurred while retrieving active maintenance" });
        }
    }

    [HttpGet("staff/workloads")]
    public async Task<IActionResult> GetStaffWorkloads()
    {
        try
        {
            var workloads = await _dashboardService.GetStaffWorkloadsAsync();
            return Ok(workloads);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting staff workloads");
            return StatusCode(500, new { error = "An error occurred while retrieving staff workloads" });
        }
    }

    [HttpGet("metrics/occupancy")]
    public async Task<IActionResult> GetOccupancyMetrics()
    {
        try
        {
            var metrics = await _dashboardService.GetOccupancyMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting occupancy metrics");
            return StatusCode(500, new { error = "An error occurred while retrieving occupancy metrics" });
        }
    }

    [HttpGet("metrics/revenue")]
    public async Task<IActionResult> GetRevenueMetrics()
    {
        try
        {
            var metrics = await _dashboardService.GetRevenueMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue metrics");
            return StatusCode(500, new { error = "An error occurred while retrieving revenue metrics" });
        }
    }

    [HttpGet("metrics/performance")]
    public async Task<IActionResult> GetPerformanceMetrics()
    {
        try
        {
            var metrics = await _dashboardService.GetPerformanceMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics");
            return StatusCode(500, new { error = "An error occurred while retrieving performance metrics" });
        }
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetSystemAlerts()
    {
        try
        {
            var alerts = await _dashboardService.GetSystemAlertsAsync();
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system alerts");
            return StatusCode(500, new { error = "An error occurred while retrieving system alerts" });
        }
    }

    [HttpGet("trends/occupancy")]
    public async Task<IActionResult> GetOccupancyTrends([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var trends = await _dashboardService.GetOccupancyTrendsAsync(startDate, endDate);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting occupancy trends");
            return StatusCode(500, new { error = "An error occurred while retrieving occupancy trends" });
        }
    }

    [HttpGet("trends/revenue")]
    public async Task<IActionResult> GetRevenueBreakdown([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var breakdown = await _dashboardService.GetRevenueBreakdownAsync(startDate, endDate);
            return Ok(breakdown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue breakdown");
            return StatusCode(500, new { error = "An error occurred while retrieving revenue breakdown" });
        }
    }

    [HttpGet("trends/service")]
    public async Task<IActionResult> GetServiceMetrics([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        try
        {
            var metrics = await _dashboardService.GetServiceMetricsAsync(startDate, endDate);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service metrics");
            return StatusCode(500, new { error = "An error occurred while retrieving service metrics" });
        }
    }
}