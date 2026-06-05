using HotelOS.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace HousekeepingService.DTOs;

public class CleaningTaskDTO
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public RoomType RoomType { get; set; }
    public RoomStatus CurrentStatus { get; set; }
    public CleaningPriority Priority { get; set; }
    public DateTime RequestedTime { get; set; }
    public DateTime? AssignedTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? CompletedTime { get; set; }
    public int? AssignedHousekeeperId { get; set; }
    public string? AssignedHousekeeperName { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? Notes { get; set; }
    public TimeSpan? EstimatedDuration { get; set; }
    public bool IsOverdue => RequestedTime.AddHours(2) < DateTime.UtcNow && CompletedTime == null;
}

public class StartCleaningRequest
{
    [Required]
    public int RoomId { get; set; }
    
    [Required]
    public int HousekeeperId { get; set; }
    
    public TimeSpan EstimatedDuration { get; set; } = TimeSpan.FromMinutes(45);
    
    public string? Notes { get; set; }
}

public class CompleteCleaningRequest
{
    [Required]
    public int RoomId { get; set; }
    
    [Required]
    public int HousekeeperId { get; set; }
    
    public bool PassedInspection { get; set; } = true;
    
    public string? Notes { get; set; }
    
    public List<string> IssuesFound { get; set; } = new List<string>();
}

public class UpdateRoomStatusRequest
{
    [Required]
    public int RoomId { get; set; }
    
    [Required]
    public RoomStatus NewStatus { get; set; }
    
    public int? StaffId { get; set; }
    
    public string? Reason { get; set; }
    
    public string? Notes { get; set; }
}

public class AssignCleaningTaskRequest
{
    [Required]
    public int RoomId { get; set; }
    
    [Required]
    public int HousekeeperId { get; set; }
    
    public CleaningPriority Priority { get; set; } = CleaningPriority.Normal;
    
    public string? SpecialInstructions { get; set; }
}

public class HousekeeperWorkloadDTO
{
    public int HousekeeperId { get; set; }
    public string HousekeeperName { get; set; } = string.Empty;
    public StaffStatus Status { get; set; }
    public int ActiveTasks { get; set; }
    public int CompletedTasks { get; set; }
    public TimeSpan TotalWorkTime { get; set; }
    public List<CleaningTaskDTO> CurrentTasks { get; set; } = new List<CleaningTaskDTO>();
}

public class RoomStatusSummaryDTO
{
    public int TotalRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int DirtyRooms { get; set; }
    public int BeingCleanedRooms { get; set; }
    public int CleanRooms { get; set; }
    public int MaintenanceRooms { get; set; }
    public int OutOfOrderRooms { get; set; }
    public int OverdueTasks { get; set; }
    public TimeSpan AverageCleaningTime { get; set; }
}

public class CleaningResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public CleaningTaskDTO? Task { get; set; }
    public Room? Room { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum CleaningPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}