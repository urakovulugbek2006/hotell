using HotelOS.Shared.Events;
using HotelOS.Shared.Infrastructure;
using DashboardService.Services;

namespace DashboardService.EventHandlers;

/// <summary>
/// Subscribes to all broker events from the other microservices and pushes
/// the corresponding real-time updates out to connected dashboard clients
/// via SignalR. This is the bridge between the message broker (Redis Pub/Sub)
/// and the WebSocket layer.
/// </summary>
public class BrokerEventHandler
{
    private readonly IMessageBroker _messageBroker;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BrokerEventHandler> _logger;

    public BrokerEventHandler(
        IMessageBroker messageBroker,
        IServiceProvider serviceProvider,
        ILogger<BrokerEventHandler> logger)
    {
        _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SubscribeToAllEventsAsync()
    {
        // Reception events
        await _messageBroker.SubscribeAsync<GuestCheckedInEvent>(EventTopics.GuestCheckedIn, HandleGuestCheckedIn);
        await _messageBroker.SubscribeAsync<GuestCheckedOutEvent>(EventTopics.GuestCheckedOut, HandleGuestCheckedOut);
        await _messageBroker.SubscribeAsync<RoomVacatedEvent>(EventTopics.RoomVacated, HandleRoomVacated);

        // Housekeeping events
        await _messageBroker.SubscribeAsync<RoomStatusChangedEvent>(EventTopics.RoomStatusChanged, HandleRoomStatusChanged);
        await _messageBroker.SubscribeAsync<RoomCleanedEvent>(EventTopics.RoomCleaned, HandleRoomCleaned);

        // Room service events
        await _messageBroker.SubscribeAsync<OrderReceivedEvent>(EventTopics.OrderReceived, HandleOrderReceived);
        await _messageBroker.SubscribeAsync<OrderPreparingEvent>(EventTopics.OrderPreparing, HandleOrderPreparing);
        await _messageBroker.SubscribeAsync<OrderOutForDeliveryEvent>(EventTopics.OrderOutForDelivery, HandleOrderOutForDelivery);
        await _messageBroker.SubscribeAsync<OrderDeliveredEvent>(EventTopics.OrderDelivered, HandleOrderDelivered);

        // Maintenance events
        await _messageBroker.SubscribeAsync<MaintenanceRequestedEvent>(EventTopics.MaintenanceRequested, HandleMaintenanceRequested);
        await _messageBroker.SubscribeAsync<MaintenanceAssignedEvent>(EventTopics.MaintenanceAssigned, HandleMaintenanceAssigned);
        await _messageBroker.SubscribeAsync<MaintenanceCompletedEvent>(EventTopics.MaintenanceCompleted, HandleMaintenanceCompleted);

        _logger.LogInformation("Dashboard subscribed to all broker events");
    }

    private async Task HandleGuestCheckedIn(GuestCheckedInEvent e)
    {
        using var scope = _serviceProvider.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        await dashboard.BroadcastCheckInOutAsync("CheckIn", e.RoomId, e.GuestName);
    }

    private async Task HandleGuestCheckedOut(GuestCheckedOutEvent e)
    {
        using var scope = _serviceProvider.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        await dashboard.BroadcastCheckInOutAsync("CheckOut", e.RoomId, e.GuestName);
    }

    private async Task HandleRoomVacated(RoomVacatedEvent e)
    {
        using var scope = _serviceProvider.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        await dashboard.BroadcastRoomStatusUpdateAsync(e.RoomId, HotelOS.Shared.Models.RoomStatus.Dirty);
    }

    private async Task HandleRoomStatusChanged(RoomStatusChangedEvent e)
    {
        using var scope = _serviceProvider.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        await dashboard.BroadcastRoomStatusUpdateAsync(e.RoomId, e.NewStatus);
    }

    private async Task HandleRoomCleaned(RoomCleanedEvent e)
    {
        using var scope = _serviceProvider.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        await dashboard.BroadcastRoomStatusUpdateAsync(e.RoomId, HotelOS.Shared.Models.RoomStatus.Clean);
    }

    private async Task HandleOrderReceived(OrderReceivedEvent e)
    {
        using var scope = _serviceProvider.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        await dashboard.BroadcastOrderUpdateAsync(e.OrderId, HotelOS.Shared.Models.OrderStatus.Received);
    }

    private async Task HandleOrderPreparing(OrderPreparingEvent e)
    {
        using var scope = _serviceProvider.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        await dashboard.BroadcastOrderUpdateAsync(e.OrderId, HotelOS.Shared.Models.OrderStatus.Preparing);
    }

    private async Task HandleOrderOutForDelivery(OrderOutForDeliveryEvent e)
    {
        using var scope = _serviceProvider.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        await dashboard.BroadcastOrderUpdateAsync(e.OrderId, HotelOS.Shared.Models.OrderStatus.OutForDelivery);
    }

    private async Task HandleOrderDelivered(OrderDeliveredEvent e)
    {
        using var scope = _serviceProvider.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        await dashboard.BroadcastOrderUpdateAsync(e.OrderId, HotelOS.Shared.Models.OrderStatus.Delivered);
    }

    private async Task HandleMaintenanceRequested(MaintenanceRequestedEvent e)
    {
        using var scope = _serviceProvider.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        await dashboard.BroadcastMaintenanceUpdateAsync(e.RequestId, HotelOS.Shared.Models.MaintenanceStatus.Reported);
    }

    private async Task HandleMaintenanceAssigned(MaintenanceAssignedEvent e)
    {
        using var scope = _serviceProvider.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        await dashboard.BroadcastMaintenanceUpdateAsync(e.RequestId, HotelOS.Shared.Models.MaintenanceStatus.Assigned);
    }

    private async Task HandleMaintenanceCompleted(MaintenanceCompletedEvent e)
    {
        using var scope = _serviceProvider.CreateScope();
        var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        await dashboard.BroadcastMaintenanceUpdateAsync(e.RequestId, HotelOS.Shared.Models.MaintenanceStatus.Completed);
    }
}