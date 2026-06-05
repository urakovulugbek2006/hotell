using System.ComponentModel.DataAnnotations;

namespace HotelOS.Shared.Models;

public class MaintenanceRequest
{
    public int Id { get; set; }
    
    public int RoomId { get; set; }
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    public MaintenancePriority Priority { get; set; }
    
    public MaintenanceStatus Status { get; set; }
    
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? AssignedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    public int? AssignedTechnicianId { get; set; }
    
    public string? ReportedBy { get; set; }
    
    public string? ResolutionNotes { get; set; }
    
    public decimal? EstimatedCost { get; set; }
    
    public decimal? ActualCost { get; set; }
    
    // Navigation properties
    public virtual Room Room { get; set; } = null!;
    public virtual Staff? AssignedTechnician { get; set; }
    
    // Computed properties
    public TimeSpan? ResponseTime => AssignedAt.HasValue ? AssignedAt.Value - ReportedAt : null;
    public TimeSpan? ResolutionTime => CompletedAt.HasValue ? CompletedAt.Value - ReportedAt : null;
}

public enum MaintenancePriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

public enum MaintenanceStatus
{
    Reported = 1,
    Assigned = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}