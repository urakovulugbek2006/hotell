using HotelOS.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace MaintenanceService.DTOs;

public class CreateMaintenanceRequestDTO
{
    [Required]
    public int RoomId { get; set; }
    
    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public MaintenancePriority Priority { get; set; }
    
    public string? ReportedBy { get; set; }
    
    public decimal? EstimatedCost { get; set; }
    
    public string? ContactPhone { get; set; }
    
    public bool IsEmergency { get; set; }
}

public class AssignMaintenanceRequestDTO
{
    [Required]
    public int RequestId { get; set; }
    
    [Required]
    public int TechnicianId { get; set; }
    
    public TimeSpan? EstimatedDuration { get; set; }
    
    public string? Notes { get; set; }
}

public class UpdateMaintenanceStatusDTO
{
    [Required]
    public int RequestId { get; set; }
    
    [Required]
    public MaintenanceStatus NewStatus { get; set; }
    
    public int? TechnicianId { get; set; }
    
    public string? Notes { get; set; }
    
    public decimal? ActualCost { get; set; }
    
    public bool? RoomBackInService { get; set; }
}

public class CompleteMaintenanceRequestDTO
{
    [Required]
    public int RequestId { get; set; }
    
    [Required]
    public int TechnicianId { get; set; }
    
    [Required]
    [StringLength(2000)]
    public string ResolutionNotes { get; set; } = string.Empty;
    
    public decimal? ActualCost { get; set; }
    
    public bool RoomBackInService { get; set; } = true;
    
    public List<string> PartsUsed { get; set; } = new List<string>();
    
    public bool RequiresFollowUp { get; set; }
    
    public DateTime? FollowUpDate { get; set; }
}

public class MaintenanceRequestDTO
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? AssignedTechnicianId { get; set; }
    public string? AssignedTechnicianName { get; set; }
    public string? ReportedBy { get; set; }
    public string? ResolutionNotes { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public TimeSpan? ResolutionTime { get; set; }
    public bool IsOverdue { get; set; }
    public int QueuePosition { get; set; }
    public TimeSpan? EstimatedWaitTime { get; set; }
}

public class TechnicianWorkloadDTO
{
    public int TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public StaffStatus Status { get; set; }
    public int ActiveRequests { get; set; }
    public int CompletedRequests { get; set; }
    public TimeSpan AverageResolutionTime { get; set; }
    public List<MaintenanceRequestDTO> CurrentRequests { get; set; } = new List<MaintenanceRequestDTO>();
    public MaintenanceSpecialty? Specialty { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime? NextAvailableTime { get; set; }
}

public class MaintenanceSummaryDTO
{
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int AssignedRequests { get; set; }
    public int InProgressRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int CancelledRequests { get; set; }
    public int CriticalRequests { get; set; }
    public int OverdueRequests { get; set; }
    public decimal TotalCost { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan AverageResolutionTime { get; set; }
    public Dictionary<MaintenancePriority, int> RequestsByPriority { get; set; } = new();
    public Dictionary<MaintenanceStatus, int> RequestsByStatus { get; set; } = new();
}

public class PriorityQueueStatusDTO
{
    public int TotalInQueue { get; set; }
    public int CriticalInQueue { get; set; }
    public int HighInQueue { get; set; }
    public int NormalInQueue { get; set; }
    public int LowInQueue { get; set; }
    public TimeSpan AverageWaitTime { get; set; }
    public TimeSpan LongestWaitTime { get; set; }
    public int AvailableTechnicians { get; set; }
    public List<MaintenanceRequestDTO> NextRequests { get; set; } = new List<MaintenanceRequestDTO>();
}

public class MaintenanceResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public MaintenanceRequestDTO? Request { get; set; }
    public int? QueuePosition { get; set; }
    public TimeSpan? EstimatedWaitTime { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum MaintenanceSpecialty
{
    General = 1,
    Plumbing = 2,
    Electrical = 3,
    HVAC = 4,
    Carpentry = 5,
    Appliances = 6,
    IT = 7,
    Security = 8
}