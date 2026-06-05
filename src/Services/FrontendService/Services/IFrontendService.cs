using FrontendService.DTOs;

namespace FrontendService.Services;

public interface IFrontendService
{
    // Guest Management
    Task<ApiResponse<GuestDTO>> CreateGuestAsync(CreateGuestDTO request);
    Task<ApiResponse<GuestDTO>> UpdateGuestAsync(UpdateGuestDTO request);
    Task<ApiResponse<GuestDTO>> GetGuestAsync(int guestId);
    Task<ApiResponse<GuestDTO>> GetGuestByEmailAsync(string email);
    Task<ApiResponse<PaginatedResponse<GuestDTO>>> GetGuestsAsync(int pageNumber = 1, int pageSize = 10);
    
    // Booking Management
    Task<ApiResponse<BookingDTO>> CreateBookingAsync(CreateBookingDTO request);
    Task<ApiResponse<BookingDTO>> GetBookingAsync(int bookingId);
    Task<ApiResponse<PaginatedResponse<BookingDTO>>> GetBookingsByGuestAsync(int guestId, int pageNumber = 1, int pageSize = 10);
    Task<ApiResponse<PaginatedResponse<BookingDTO>>> GetActiveBookingsAsync(int pageNumber = 1, int pageSize = 10);
    Task<ApiResponse<BookingDTO>> CancelBookingAsync(int bookingId, string reason);
    Task<ApiResponse<BookingDTO>> ModifyBookingAsync(int bookingId, CreateBookingDTO modifications);
    
    // Room Availability
    Task<ApiResponse<PaginatedResponse<RoomDTO>>> GetAvailableRoomsAsync(RoomAvailabilityQueryDTO query, int pageNumber = 1, int pageSize = 10);
    Task<ApiResponse<RoomDTO>> GetRoomAsync(int roomId);
    Task<ApiResponse<decimal>> GetRoomRateAsync(int roomId, DateTime checkIn, DateTime checkOut);
    
    // Room Service
    Task<ApiResponse<OrderDTO>> CreateRoomServiceOrderAsync(CreateOrderDTO request);
    Task<ApiResponse<OrderDTO>> GetOrderAsync(int orderId);
    Task<ApiResponse<PaginatedResponse<OrderDTO>>> GetOrdersByRoomAsync(int roomId, int pageNumber = 1, int pageSize = 10);
    Task<ApiResponse<OrderDTO>> CancelOrderAsync(int orderId, string reason);
    Task<ApiResponse<PaginatedResponse<MenuItemDTO>>> GetMenuAsync(MenuCategory? category = null);
    
    // Maintenance Requests
    Task<ApiResponse<MaintenanceRequestDTO>> CreateMaintenanceRequestAsync(CreateMaintenanceRequestDTO request);
    Task<ApiResponse<MaintenanceRequestDTO>> GetMaintenanceRequestAsync(int requestId);
    Task<ApiResponse<PaginatedResponse<MaintenanceRequestDTO>>> GetMaintenanceRequestsByRoomAsync(int roomId, int pageNumber = 1, int pageSize = 10);
    
    // Billing
    Task<ApiResponse<BillDTO>> GetBillAsync(int bookingId);
    Task<ApiResponse<PaginatedResponse<BillDTO>>> GetBillsByGuestAsync(int guestId, int pageNumber = 1, int pageSize = 10);
    
    // Authentication
    Task<LoginResponse> AuthenticateGuestAsync(LoginRequest request);
    Task<ApiResponse<GuestDTO>> RegisterGuestAsync(CreateGuestDTO request);
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
    public List<string> Allergens { get; set; } = new();
}