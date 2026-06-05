using HotelOS.Shared.Models;
using RoomService.DTOs;

namespace RoomService.Services;

public interface IRoomService
{
    // Order Management
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderResponse> UpdateOrderStatusAsync(UpdateOrderStatusRequest request);
    Task<OrderResponse> AssignOrderAsync(AssignOrderRequest request);
    Task<OrderResponse> CancelOrderAsync(int orderId, string reason);
    
    // Order Workflow
    Task<OrderResponse> StartPreparationAsync(int orderId, int chefId);
    Task<OrderResponse> CompletePreparationAsync(int orderId, int chefId);
    Task<OrderResponse> StartDeliveryAsync(int orderId, int deliveryStaffId);
    Task<OrderResponse> CompleteDeliveryAsync(int orderId, int deliveryStaffId);
    
    // Order Queries
    Task<IEnumerable<OrderDTO>> GetOrdersByStatusAsync(OrderStatus status);
    Task<IEnumerable<OrderDTO>> GetOrdersByRoomAsync(int roomId);
    Task<IEnumerable<OrderDTO>> GetOrdersByStaffAsync(int staffId);
    Task<IEnumerable<OrderDTO>> GetOverdueOrdersAsync();
    Task<OrderDTO?> GetOrderAsync(int orderId);
    
    // Menu Management
    Task<IEnumerable<MenuItemDTO>> GetMenuItemsAsync(MenuCategory? category = null);
    Task<MenuItemDTO?> GetMenuItemAsync(int menuItemId);
    
    // Kitchen Management
    Task<IEnumerable<KitchenWorkloadDTO>> GetKitchenWorkloadAsync();
    Task<KitchenWorkloadDTO?> GetStaffWorkloadAsync(int staffId);
    Task<IEnumerable<Staff>> GetAvailableKitchenStaffAsync();
    
    // Reporting
    Task<OrderSummaryDTO> GetOrderSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
}