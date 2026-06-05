using HotelOS.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using RoomService.DTOs;
using RoomService.Services;

namespace RoomService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomServiceController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly ILogger<RoomServiceController> _logger;

    public RoomServiceController(IRoomService roomService, ILogger<RoomServiceController> logger)
    {
        _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            _logger.LogInformation("Creating room service order for room {RoomId} with {ItemCount} items", 
                request.RoomId, request.Items.Count);

            var result = await _roomService.CreateOrderAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            _logger.LogWarning("Failed to create order: {Error}", result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room service order");
            return StatusCode(500, new { error = "An error occurred while creating the order" });
        }
    }

    [HttpPut("orders/{orderId}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            request.OrderId = orderId; // Ensure consistency
            var result = await _roomService.UpdateOrderStatusAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for order {OrderId}", orderId);
            return StatusCode(500, new { error = "An error occurred while updating order status" });
        }
    }

    [HttpPost("orders/{orderId}/assign")]
    public async Task<IActionResult> AssignOrder(int orderId, [FromBody] AssignOrderRequest request)
    {
        try
        {
            request.OrderId = orderId;
            var result = await _roomService.AssignOrderAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning order {OrderId}", orderId);
            return StatusCode(500, new { error = "An error occurred while assigning the order" });
        }
    }

    [HttpPost("orders/{orderId}/cancel")]
    public async Task<IActionResult> CancelOrder(int orderId, [FromBody] CancelOrderRequest request)
    {
        try
        {
            var result = await _roomService.CancelOrderAsync(orderId, request.Reason);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            return StatusCode(500, new { error = "An error occurred while cancelling the order" });
        }
    }

    [HttpPost("orders/{orderId}/start-preparation")]
    public async Task<IActionResult> StartPreparation(int orderId, [FromBody] StartPreparationRequest request)
    {
        try
        {
            var result = await _roomService.StartPreparationAsync(orderId, request.ChefId);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting preparation for order {OrderId}", orderId);
            return StatusCode(500, new { error = "An error occurred while starting preparation" });
        }
    }

    [HttpPost("orders/{orderId}/complete-preparation")]
    public async Task<IActionResult> CompletePreparation(int orderId, [FromBody] CompletePreparationRequest request)
    {
        try
        {
            var result = await _roomService.CompletePreparationAsync(orderId, request.ChefId);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing preparation for order {OrderId}", orderId);
            return StatusCode(500, new { error = "An error occurred while completing preparation" });
        }
    }

    [HttpPost("orders/{orderId}/start-delivery")]
    public async Task<IActionResult> StartDelivery(int orderId, [FromBody] StartDeliveryRequest request)
    {
        try
        {
            var result = await _roomService.StartDeliveryAsync(orderId, request.DeliveryStaffId);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting delivery for order {OrderId}", orderId);
            return StatusCode(500, new { error = "An error occurred while starting delivery" });
        }
    }

    [HttpPost("orders/{orderId}/complete-delivery")]
    public async Task<IActionResult> CompleteDelivery(int orderId, [FromBody] CompleteDeliveryRequest request)
    {
        try
        {
            var result = await _roomService.CompleteDeliveryAsync(orderId, request.DeliveryStaffId);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing delivery for order {OrderId}", orderId);
            return StatusCode(500, new { error = "An error occurred while completing delivery" });
        }
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] OrderStatus? status = null, [FromQuery] int? roomId = null, [FromQuery] int? staffId = null)
    {
        try
        {
            IEnumerable<OrderDTO> orders;

            if (status.HasValue)
            {
                orders = await _roomService.GetOrdersByStatusAsync(status.Value);
            }
            else if (roomId.HasValue)
            {
                orders = await _roomService.GetOrdersByRoomAsync(roomId.Value);
            }
            else if (staffId.HasValue)
            {
                orders = await _roomService.GetOrdersByStaffAsync(staffId.Value);
            }
            else
            {
                // Return all recent orders if no filter specified
                orders = await _roomService.GetOrdersByStatusAsync(OrderStatus.Received);
            }

            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching orders");
            return StatusCode(500, new { error = "An error occurred while fetching orders" });
        }
    }

    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetOrder(int orderId)
    {
        try
        {
            var order = await _roomService.GetOrderAsync(orderId);
            
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order {OrderId}", orderId);
            return StatusCode(500, new { error = "An error occurred while fetching the order" });
        }
    }

    [HttpGet("orders/overdue")]
    public async Task<IActionResult> GetOverdueOrders()
    {
        try
        {
            var orders = await _roomService.GetOverdueOrdersAsync();
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching overdue orders");
            return StatusCode(500, new { error = "An error occurred while fetching overdue orders" });
        }
    }

    [HttpGet("menu")]
    public async Task<IActionResult> GetMenu([FromQuery] MenuCategory? category = null)
    {
        try
        {
            var menuItems = await _roomService.GetMenuItemsAsync(category);
            return Ok(menuItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching menu items");
            return StatusCode(500, new { error = "An error occurred while fetching menu items" });
        }
    }

    [HttpGet("menu/{menuItemId}")]
    public async Task<IActionResult> GetMenuItem(int menuItemId)
    {
        try
        {
            var menuItem = await _roomService.GetMenuItemAsync(menuItemId);
            
            if (menuItem == null)
            {
                return NotFound(new { message = "Menu item not found" });
            }

            return Ok(menuItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching menu item {MenuItemId}", menuItemId);
            return StatusCode(500, new { error = "An error occurred while fetching the menu item" });
        }
    }

    [HttpGet("kitchen/workload")]
    public async Task<IActionResult> GetKitchenWorkload()
    {
        try
        {
            var workload = await _roomService.GetKitchenWorkloadAsync();
            return Ok(workload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching kitchen workload");
            return StatusCode(500, new { error = "An error occurred while fetching kitchen workload" });
        }
    }

    [HttpGet("kitchen/staff/{staffId}/workload")]
    public async Task<IActionResult> GetStaffWorkload(int staffId)
    {
        try
        {
            var workload = await _roomService.GetStaffWorkloadAsync(staffId);
            
            if (workload == null)
            {
                return NotFound(new { message = "Staff member not found" });
            }

            return Ok(workload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching staff workload for {StaffId}", staffId);
            return StatusCode(500, new { error = "An error occurred while fetching staff workload" });
        }
    }

    [HttpGet("kitchen/staff/available")]
    public async Task<IActionResult> GetAvailableKitchenStaff()
    {
        try
        {
            var staff = await _roomService.GetAvailableKitchenStaffAsync();
            return Ok(staff);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available kitchen staff");
            return StatusCode(500, new { error = "An error occurred while fetching available kitchen staff" });
        }
    }

    [HttpGet("reports/summary")]
    public async Task<IActionResult> GetOrderSummary([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var summary = await _roomService.GetOrderSummaryAsync(startDate, endDate);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order summary");
            return StatusCode(500, new { error = "An error occurred while fetching order summary" });
        }
    }
}

// Additional DTOs for controller endpoints
public class CancelOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class StartPreparationRequest
{
    public int ChefId { get; set; }
}

public class CompletePreparationRequest
{
    public int ChefId { get; set; }
}

public class StartDeliveryRequest
{
    public int DeliveryStaffId { get; set; }
}

public class CompleteDeliveryRequest
{
    public int DeliveryStaffId { get; set; }
}