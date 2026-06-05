using HotelOS.Shared.Events;

namespace HotelOS.Shared.Infrastructure;

public interface IMessageBroker
{
    Task PublishAsync<T>(T eventData, string topic) where T : IEvent;
    Task SubscribeAsync<T>(string topic, Func<T, Task> handler) where T : IEvent;
    Task UnsubscribeAsync(string topic);
    Task<bool> IsConnectedAsync();
    Task DisconnectAsync();
}

public interface IEventHandler<in T> where T : IEvent
{
    Task HandleAsync(T eventData);
}

public class EventTopics
{
    // Reception Service Events
    public const string GuestCheckedIn = "reception.guest.checkedin";
    public const string GuestCheckedOut = "reception.guest.checkedout";
    public const string RoomAssigned = "reception.room.assigned";
    public const string RoomVacated = "reception.room.vacated";
    
    // Housekeeping Service Events
    public const string RoomCleaningStarted = "housekeeping.room.cleaning.started";
    public const string RoomCleaned = "housekeeping.room.cleaned";
    public const string RoomNeedsCleaning = "housekeeping.room.needs.cleaning";
    public const string RoomStatusChanged = "housekeeping.room.status.changed";
    
    // Room Service Events
    public const string OrderReceived = "roomservice.order.received";
    public const string OrderPreparing = "roomservice.order.preparing";
    public const string OrderOutForDelivery = "roomservice.order.outfordelivery";
    public const string OrderDelivered = "roomservice.order.delivered";
    public const string OrderCancelled = "roomservice.order.cancelled";
    
    // Maintenance Service Events
    public const string MaintenanceRequested = "maintenance.request.created";
    public const string MaintenanceAssigned = "maintenance.request.assigned";
    public const string MaintenanceStarted = "maintenance.work.started";
    public const string MaintenanceCompleted = "maintenance.work.completed";
    
    // Dashboard Events (for real-time updates)
    public const string DashboardUpdate = "dashboard.update";
}