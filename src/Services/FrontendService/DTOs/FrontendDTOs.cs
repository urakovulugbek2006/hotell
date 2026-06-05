using HotelOS.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace FrontendService.DTOs;

// Guest Management DTOs
public class CreateGuestDTO
{
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
    
    public string? Address { get; set; }
    
    [Required]
    public DateTime DateOfBirth { get; set; }
    
    public string? PassportNumber { get; set; }
    
    public string? Nationality { get; set; }
    
    public string? SpecialRequests { get; set; }
}

public class UpdateGuestDTO
{
    [Required]
    public int Id { get; set; }
    
    [StringLength(100)]
    public string? FirstName { get; set; }
    
    [StringLength(100)]
    public string? LastName { get; set; }
    
    [EmailAddress]
    public string? Email { get; set; }
    
    [Phone]
    public string? PhoneNumber { get; set; }
    
    public string? Address { get; set; }
    
    public string? PassportNumber { get; set; }
    
    public string? Nationality { get; set; }
    
    public string? SpecialRequests { get; set; }
}

public class GuestDTO
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? PassportNumber { get; set; }
    public string? Nationality { get; set; }
    public bool IsVip { get; set; }
    public string? SpecialRequests { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Booking DTOs
public class CreateBookingDTO
{
    [Required]
    public int GuestId { get; set; }
    
    [Required]
    public RoomType RequestedRoomType { get; set; }
    
    [Required]
    public DateTime CheckInDate { get; set; }
    
    [Required]
    public DateTime CheckOutDate { get; set; }
    
    public int? FloorPreference { get; set; }
    
    public bool NeedElevatorAccess { get; set; }
    
    public bool NeedStairsAccess { get; set; }
    
    public string? SpecialRequests { get; set; }
}

public class BookingDTO
{
    public int Id { get; set; }
    public int GuestId { get; set; }
    public GuestDTO Guest { get; set; } = new();
    public int? RoomId { get; set; }
    public RoomDTO? Room { get; set; }
    public DateTime CheckInDate { get; set; }
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
    public decimal BalanceDue { get; set; }
    public int NumberOfNights { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Room DTOs
public class RoomAvailabilityQueryDTO
{
    [Required]
    public DateTime CheckInDate { get; set; }
    
    [Required]
    public DateTime CheckOutDate { get; set; }
    
    public RoomType? RoomType { get; set; }
    
    public int? Floor { get; set; }
    
    public bool? AccessibleOnly { get; set; }
    
    public bool? NearElevator { get; set; }
    
    public bool? NearStairs { get; set; }
    
    public decimal? MaxRate { get; set; }
}

public class RoomDTO
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public RoomType Type { get; set; }
    public RoomStatus Status { get; set; }
    public decimal NightlyRate { get; set; }
    public bool IsAccessible { get; set; }
    public bool NearElevator { get; set; }
    public bool NearStairs { get; set; }
    public DateTime? LastCleaned { get; set; }
    public string RoomTypeDescription { get; set; } = string.Empty;
    public List<string> Amenities { get; set; } = new();
    public bool IsAvailable { get; set; }
}

// Room Service DTOs
public class CreateOrderDTO
{
    [Required]
    public int RoomId { get; set; }
    
    [Required]
    public string GuestName { get; set; } = string.Empty;
    
    public int? BookingId { get; set; }
    
    [Required]
    public List<OrderItemDTO> Items { get; set; } = new();
    
    public string? SpecialInstructions { get; set; }
    
    public DateTime? PreferredDeliveryTime { get; set; }
}

public class OrderItemDTO
{
    [Required]
    public string ItemName { get; set; } = string.Empty;
    
    [Range(1, 10)]
    public int Quantity { get; set; }
    
    [Range(0.01, 1000.00)]
    public decimal UnitPrice { get; set; }
    
    public string? Notes { get; set; }
}

public class OrderDTO
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime OrderTime { get; set; }
    public DateTime? PreparedTime { get; set; }
    public DateTime? DeliveredTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<OrderItemDTO> Items { get; set; } = new();
    public string StatusDescription { get; set; } = string.Empty;
    public TimeSpan? EstimatedDeliveryTime { get; set; }
}

// Maintenance Request DTOs
public class CreateMaintenanceRequestDTO
{
    [Required]
    public int RoomId { get; set; }
    
    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;
    
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Normal;
    
    public string? ReportedBy { get; set; }
    
    public string? ContactPhone { get; set; }
    
    public bool IsEmergency { get; set; }
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
    public string? AssignedTechnicianName { get; set; }
    public string? ReportedBy { get; set; }
    public string? ResolutionNotes { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public string PriorityDescription { get; set; } = string.Empty;
    public TimeSpan? EstimatedResolutionTime { get; set; }
}

// Bill DTOs
public class BillDTO
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public decimal RoomCharges { get; set; }
    public decimal RoomServiceCharges { get; set; }
    public decimal AdditionalCharges { get; set; }
    public decimal Taxes { get; set; }
    public decimal Discounts { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
    public BillStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public List<BillLineItemDTO> LineItems { get; set; } = new();
}

public class BillLineItemDTO
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime ChargeDate { get; set; }
    public ChargeType ChargeType { get; set; }
}

// Response DTOs
public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public static ApiResponse<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static ApiResponse<T> Error(string message) => new() { IsSuccess = false, ErrorMessage = message };
}

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

// Authentication DTOs
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Token { get; set; }
    public GuestDTO? Guest { get; set; }
    public DateTime? ExpiresAt { get; set; }
}