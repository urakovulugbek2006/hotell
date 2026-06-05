using System.ComponentModel.DataAnnotations;

namespace HotelOS.Shared.Models;

public class Booking
{
    public int Id { get; set; }
    
    public int GuestId { get; set; }
    
    public int? RoomId { get; set; }
    
    [Required]
    public DateTime CheckInDate { get; set; }
    
    [Required]
    public DateTime CheckOutDate { get; set; }
    
    public DateTime? ActualCheckIn { get; set; }
    
    public DateTime? ActualCheckOut { get; set; }
    
    public BookingStatus Status { get; set; }
    
    public RoomType RequestedRoomType { get; set; }
    
    public int? FloorPreference { get; set; }
    
    public bool NeedElevatorAccess { get; set; }
    
    public bool NeedStairsAccess { get; set; }
    
    public string? SpecialRequests { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    public decimal PaidAmount { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Guest Guest { get; set; } = null!;
    public virtual Room? Room { get; set; }
    public virtual Bill? Bill { get; set; }
    
    // Computed properties
    public int NumberOfNights => (CheckOutDate - CheckInDate).Days;
    public decimal BalanceDue => TotalAmount - PaidAmount;
    public bool IsActive => Status == BookingStatus.CheckedIn;
}

public enum BookingStatus
{
    Pending = 1,
    Confirmed = 2,
    CheckedIn = 3,
    CheckedOut = 4,
    Cancelled = 5,
    NoShow = 6
}