using FrontendService.DTOs;
using FrontendService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontendService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IFrontendService _frontendService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IFrontendService frontendService, ILogger<OrderController> logger)
    {
        _frontendService = frontendService ?? throw new ArgumentNullException(nameof(frontendService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDTO request)
    {
        try
        {
            var result = await _frontendService.CreateRoomServiceOrderAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room service order");
            return StatusCode(500, new { error = "An error occurred while creating the order" });
        }
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrder(int orderId)
    {
        try
        {
            var result = await _frontendService.GetOrderAsync(orderId);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", orderId);
            return StatusCode(500, new { error = "An error occurred while retrieving the order" });
        }
    }

    [HttpGet("room/{roomId}")]
    public async Task<IActionResult> GetOrdersByRoom(int roomId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _frontendService.GetOrdersByRoomAsync(roomId, pageNumber, pageSize);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for room {RoomId}", roomId);
            return StatusCode(500, new { error = "An error occurred while retrieving orders" });
        }
    }

    [HttpPost("{orderId}/cancel")]
    public async Task<IActionResult> CancelOrder(int orderId, [FromBody] CancelOrderRequest request)
    {
        try
        {
            var result = await _frontendService.CancelOrderAsync(orderId, request.Reason);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            return StatusCode(500, new { error = "An error occurred while cancelling the order" });
        }
    }

    [HttpGet("menu")]
    public async Task<IActionResult> GetMenu([FromQuery] MenuCategory? category = null)
    {
        try
        {
            var result = await _frontendService.GetMenuAsync(category);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving menu");
            return StatusCode(500, new { error = "An error occurred while retrieving the menu" });
        }
    }
}

public class CancelOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}