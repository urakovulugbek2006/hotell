using HotelOS.Shared.Events;
using HotelOS.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace HousekeepingService.Models;

public class CleaningTask
{
    public int Id { get; set; }
    
    public int RoomId { get; set; }
    
    public CleaningPriority Priority { get; set; }
    
    public CleaningTaskStatus Status { get; set; }
    
    public DateTime RequestedTime { get; set; } = DateTime.UtcNow;
    
    public DateTime? AssignedTime { get; set; }
    
    public DateTime? StartTime { get; set; }
    
    public DateTime? CompletedTime { get; set; }
    
    public int? AssignedHousekeeperId { get; set; }
    
    public string? SpecialInstructions { get; set; }
    
    public string? Notes { get; set; }
    
    public TimeSpan? EstimatedDuration { get; set; }
    
    public TimeSpan? ActualDuration => CompletedTime.HasValue && StartTime.HasValue 
        ? CompletedTime.Value - StartTime.Value 
        : null;
    
    public bool PassedInspection { get; set; } = true;
    
    public List<string> IssuesFound { get; set; } = new List<string>();
    
    // Navigation properties
    public virtual Room Room { get; set; } = null!;
    public virtual Staff? AssignedHousekeeper { get; set; }
    
    // Computed properties
    public bool IsOverdue => RequestedTime.AddHours(2) < DateTime.UtcNow && Status != CleaningTaskStatus.Completed;
    public bool IsInProgress => Status == CleaningTaskStatus.InProgress;
    public bool IsCompleted => Status == CleaningTaskStatus.Completed;
}

public enum CleaningTaskStatus
{
    Pending = 1,
    Assigned = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    Failed = 6
}