using System.ComponentModel.DataAnnotations;

namespace HotelOS.Shared.Models;

public class RoomServiceOrder
{
    public int Id { get; set; }
    
    public int RoomId { get; set; }
    
    public int? BookingId { get; set; }
    
    [Required]
    public string GuestName { get; set; } = string.Empty;
    
    public OrderStatus Status { get; set; }
    
    public DateTime OrderTime { get; set; } = DateTime.UtcNow;
    
    public DateTime? PreparedTime { get; set; }
    
    public DateTime? DeliveredTime { get; set; }
    
    public string? SpecialInstructions { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    public int? AssignedStaffId { get; set; }
    
    // Navigation properties
    public virtual Room Room { get; set; } = null!;
    public virtual Booking? Booking { get; set; }
    public virtual Staff? AssignedStaff { get; set; }
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    
    [Required]
    public string ItemName { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    
    public decimal UnitPrice { get; set; }
    
    public decimal TotalPrice => Quantity * UnitPrice;
    
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual RoomServiceOrder Order { get; set; } = null!;
}

public enum OrderStatus
{
    Received = 1,
    Preparing = 2,
    OutForDelivery = 3,
    Delivered = 4,
    Cancelled = 5
}