namespace HotelOS.Shared.Events;

public interface IEvent
{
    string EventId { get; }
    DateTime Timestamp { get; }
    string EventType { get; }
}

public abstract class BaseEvent : IEvent
{
    public string EventId { get; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public abstract string EventType { get; }
}