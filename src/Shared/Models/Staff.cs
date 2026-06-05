using System.ComponentModel.DataAnnotations;

namespace HotelOS.Shared.Models;

public class Staff
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Phone]
    public string? PhoneNumber { get; set; }
    
    public StaffRole Role { get; set; }
    
    public StaffStatus Status { get; set; }
    
    public DateTime HireDate { get; set; }
    
    public string? EmployeeId { get; set; }
    
    public decimal? HourlyRate { get; set; }
    
    public string? Department { get; set; }
    
    public int? SupervisorId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Staff? Supervisor { get; set; }
    public virtual ICollection<Staff> Subordinates { get; set; } = new List<Staff>();
    public virtual ICollection<RoomServiceOrder> RoomServiceOrders { get; set; } = new List<RoomServiceOrder>();
    public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    
    // Computed property
    public string FullName => $"{FirstName} {LastName}";
}

public enum StaffRole
{
    Manager = 1,
    Receptionist = 2,
    Housekeeper = 3,
    HousekeepingSupervisor = 4,
    RoomServiceStaff = 5,
    Chef = 6,
    Technician = 7,
    MaintenanceSupervisor = 8,
    Security = 9
}

public enum StaffStatus
{
    Active = 1,
    OnBreak = 2,
    OnShift = 3,
    OffDuty = 4,
    Sick = 5,
    Vacation = 6,
    Terminated = 7
}