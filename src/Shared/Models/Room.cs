using System.ComponentModel.DataAnnotations;

namespace HotelOS.Shared.Models;

public class Room
{
    public int Id { get; set; }
    
    [Required]
    public string RoomNumber { get; set; } = string.Empty;
    
    public int Floor { get; set; }
    
    [Required]
    public RoomType Type { get; set; }
    
    public RoomStatus Status { get; set; }
    
    public DateTime? LastCleaned { get; set; }
    
    public decimal NightlyRate { get; set; }
    
    public bool IsAccessible { get; set; }
    
    public string? FloorPreference { get; set; }
    
    public bool NearElevator { get; set; }
    
    public bool NearStairs { get; set; }
    
    // Navigation properties
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public virtual ICollection<RoomServiceOrder> RoomServiceOrders { get; set; } = new List<RoomServiceOrder>();
    public virtual ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
}

public enum RoomType
{
    Single = 1,
    Double = 2,
    Suite = 3,
    Accessible = 4
}

public enum RoomStatus
{
    Available = 1,
    Occupied = 2,
    Dirty = 3,
    BeingCleaned = 4,
    Clean = 5,
    Maintenance = 6,
    OutOfOrder = 7
}