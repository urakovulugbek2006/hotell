using HotelOS.Shared.Models;

namespace HotelOS.Shared.Events;

public class MaintenanceRequestedEvent : BaseEvent
{
    public override string EventType => "MaintenanceRequested";
    
    public int RequestId { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MaintenancePriority Priority { get; set; }
    public DateTime ReportedTime { get; set; }
    public string ReportedBy { get; set; } = string.Empty;
    public decimal? EstimatedCost { get; set; }
}

public class MaintenanceAssignedEvent : BaseEvent
{
    public override string EventType => "MaintenanceAssigned";
    
    public int RequestId { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public DateTime AssignedTime { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
    public MaintenancePriority Priority { get; set; }
}

public class MaintenanceStartedEvent : BaseEvent
{
    public override string EventType => "MaintenanceStarted";
    
    public int RequestId { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
}

public class MaintenanceCompletedEvent : BaseEvent
{
    public override string EventType => "MaintenanceCompleted";
    
    public int RequestId { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public DateTime CompletedTime { get; set; }
    public TimeSpan ActualDuration { get; set; }
    public string? ResolutionNotes { get; set; }
    public decimal? ActualCost { get; set; }
    public bool RoomBackInService { get; set; } = true;
}