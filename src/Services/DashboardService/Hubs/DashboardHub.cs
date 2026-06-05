using Microsoft.AspNetCore.SignalR;
using DashboardService.Services;

namespace DashboardService.Hubs;

public class DashboardHub : Hub
{
    private readonly ILogger<DashboardHub> _logger;
    private readonly IDashboardService _dashboardService;

    public DashboardHub(ILogger<DashboardHub> logger, IDashboardService dashboardService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
    }

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var userAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString();
        
        _logger.LogInformation("Dashboard client connected: {ConnectionId}, UserAgent: {UserAgent}", 
            connectionId, userAgent);

        // Join the dashboard group for broadcasts
        await Groups.AddToGroupAsync(connectionId, "Dashboard");
        
        // Send initial dashboard data
        try
        {
            var overview = await _dashboardService.GetHotelOverviewAsync();
            var roomStatus = await _dashboardService.GetRoomStatusDashboardAsync();
            var alerts = await _dashboardService.GetSystemAlertsAsync();
            
            await Clients.Caller.SendAsync("InitialData", new
            {
                Overview = overview,
                RoomStatus = roomStatus,
                Alerts = alerts,
                ConnectedAt = DateTime.UtcNow
            });
            
            _logger.LogInformation("Sent initial dashboard data to client {ConnectionId}", connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending initial data to client {ConnectionId}", connectionId);
            await Clients.Caller.SendAsync("Error", "Failed to load initial dashboard data");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        
        if (exception != null)
        {
            _logger.LogWarning(exception, "Dashboard client disconnected with error: {ConnectionId}", connectionId);
        }
        else
        {
            _logger.LogInformation("Dashboard client disconnected: {ConnectionId}", connectionId);
        }

        await Groups.RemoveFromGroupAsync(connectionId, "Dashboard");
        await base.OnDisconnectedAsync(exception);
    }

    // Client can request specific data updates
    public async Task RequestRoomStatus()
    {
        try
        {
            var roomStatus = await _dashboardService.GetRoomStatusDashboardAsync();
            await Clients.Caller.SendAsync("RoomStatusUpdate", roomStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending room status to client {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", "Failed to load room status");
        }
    }

    public async Task RequestActiveOrders()
    {
        try
        {
            var orders = await _dashboardService.GetActiveOrdersAsync();
            await Clients.Caller.SendAsync("ActiveOrdersUpdate", orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending active orders to client {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", "Failed to load active orders");
        }
    }

    public async Task RequestMaintenanceStatus()
    {
        try
        {
            var maintenance = await _dashboardService.GetActiveMaintenanceAsync();
            await Clients.Caller.SendAsync("MaintenanceUpdate", maintenance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending maintenance status to client {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", "Failed to load maintenance status");
        }
    }

    public async Task RequestMetrics()
    {
        try
        {
            var occupancy = await _dashboardService.GetOccupancyMetricsAsync();
            var revenue = await _dashboardService.GetRevenueMetricsAsync();
            var performance = await _dashboardService.GetPerformanceMetricsAsync();
            
            await Clients.Caller.SendAsync("MetricsUpdate", new
            {
                Occupancy = occupancy,
                Revenue = revenue,
                Performance = performance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending metrics to client {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", "Failed to load metrics");
        }
    }

    public async Task AcknowledgeAlert(int alertId)
    {
        try
        {
            // In a real implementation, you would update the alert status in the database
            _logger.LogInformation("Alert {AlertId} acknowledged by client {ConnectionId}", alertId, Context.ConnectionId);
            
            // Broadcast the acknowledgment to all connected clients
            await Clients.Group("Dashboard").SendAsync("AlertAcknowledged", new
            {
                AlertId = alertId,
                AcknowledgedAt = DateTime.UtcNow,
                AcknowledgedBy = "Dashboard User" // In real implementation, get from authentication
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
            await Clients.Caller.SendAsync("Error", "Failed to acknowledge alert");
        }
    }

    // Subscribe to specific room updates
    public async Task SubscribeToRoom(string roomNumber)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Room-{roomNumber}");
        _logger.LogInformation("Client {ConnectionId} subscribed to room {RoomNumber}", 
            Context.ConnectionId, roomNumber);
    }

    public async Task UnsubscribeFromRoom(string roomNumber)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Room-{roomNumber}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from room {RoomNumber}", 
            Context.ConnectionId, roomNumber);
    }

    // Heartbeat to keep connection alive
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
    }
}