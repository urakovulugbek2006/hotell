using HotelOS.Shared.Models;

namespace HotelOS.Shared.Events;

public class RoomCleaningStartedEvent : BaseEvent
{
    public override string EventType => "RoomCleaningStarted";
    
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int HousekeeperId { get; set; }
    public string HousekeeperName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
}

public class RoomCleanedEvent : BaseEvent
{
    public override string EventType => "RoomCleaned";
    
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int HousekeeperId { get; set; }
    public string HousekeeperName { get; set; } = string.Empty;
    public DateTime CompletedTime { get; set; }
    public TimeSpan ActualDuration { get; set; }
    public string? Notes { get; set; }
    public bool PassedInspection { get; set; } = true;
}

public class RoomNeedsCleaningEvent : BaseEvent
{
    public override string EventType => "RoomNeedsCleaning";
    
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime RequestedTime { get; set; }
    public CleaningPriority Priority { get; set; }
    public string? SpecialInstructions { get; set; }
}

public class RoomStatusChangedEvent : BaseEvent
{
    public override string EventType => "RoomStatusChanged";
    
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public RoomStatus PreviousStatus { get; set; }
    public RoomStatus NewStatus { get; set; }
    public DateTime ChangedTime { get; set; }
    public int? ChangedByStaffId { get; set; }
    public string? ChangedByStaffName { get; set; }
    public string? Reason { get; set; }
}

public enum CleaningPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}