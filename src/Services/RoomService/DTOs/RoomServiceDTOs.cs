using HotelOS.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace RoomService.DTOs;

public class CreateOrderRequest
{
    [Required]
    public int RoomId { get; set; }
    
    [Required]
    public string GuestName { get; set; } = string.Empty;
    
    public int? BookingId { get; set; }
    
    [Required]
    public List<OrderItemRequest> Items { get; set; } = new List<OrderItemRequest>();
    
    public string? SpecialInstructions { get; set; }
}

public class OrderItemRequest
{
    [Required]
    public string ItemName { get; set; } = string.Empty;
    
    [Range(1, 100)]
    public int Quantity { get; set; }
    
    [Range(0.01, 1000.00)]
    public decimal UnitPrice { get; set; }
    
    public string? Notes { get; set; }
}

public class UpdateOrderStatusRequest
{
    [Required]
    public int OrderId { get; set; }
    
    [Required]
    public OrderStatus NewStatus { get; set; }
    
    public int? StaffId { get; set; }
    
    public string? Notes { get; set; }
    
    public TimeSpan? EstimatedTime { get; set; }
}

public class AssignOrderRequest
{
    [Required]
    public int OrderId { get; set; }
    
    [Required]
    public int StaffId { get; set; }
    
    public TimeSpan EstimatedPreparationTime { get; set; } = TimeSpan.FromMinutes(30);
}

public class OrderDTO
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public int? BookingId { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime OrderTime { get; set; }
    public DateTime? PreparedTime { get; set; }
    public DateTime? DeliveredTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string? SpecialInstructions { get; set; }
    public int? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }
    public List<OrderItemDTO> Items { get; set; } = new List<OrderItemDTO>();
    public TimeSpan? PreparationTime => PreparedTime.HasValue ? PreparedTime.Value - OrderTime : null;
    public TimeSpan? DeliveryTime => DeliveredTime.HasValue && PreparedTime.HasValue ? 
        DeliveredTime.Value - PreparedTime.Value : null;
    public TimeSpan? TotalOrderTime => DeliveredTime.HasValue ? DeliveredTime.Value - OrderTime : null;
    public bool IsOverdue => Status != OrderStatus.Delivered && OrderTime.AddHours(1) < DateTime.UtcNow;
}

public class OrderItemDTO
{
    public int Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
}

public class MenuItemDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public MenuCategory Category { get; set; }
    public bool IsAvailable { get; set; }
    public string? ImageUrl { get; set; }
    public int PreparationTimeMinutes { get; set; }
    public List<string> Allergens { get; set; } = new List<string>();
}

public class KitchenWorkloadDTO
{
    public int StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public StaffRole Role { get; set; }
    public StaffStatus Status { get; set; }
    public int ActiveOrders { get; set; }
    public int CompletedOrders { get; set; }
    public TimeSpan AveragePreparationTime { get; set; }
    public List<OrderDTO> CurrentOrders { get; set; } = new List<OrderDTO>();
}

public class OrderSummaryDTO
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int PreparingOrders { get; set; }
    public int OutForDeliveryOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int OverdueOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public TimeSpan AveragePreparationTime { get; set; }
    public TimeSpan AverageDeliveryTime { get; set; }
}

public class OrderResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public OrderDTO? Order { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum MenuCategory
{
    Appetizers = 1,
    MainCourse = 2,
    Desserts = 3,
    Beverages = 4,
    Breakfast = 5,
    LateNight = 6
}