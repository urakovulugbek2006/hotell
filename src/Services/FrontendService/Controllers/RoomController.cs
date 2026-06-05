using FrontendService.DTOs;
using FrontendService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontendService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomController : ControllerBase
{
    private readonly IFrontendService _frontendService;
    private readonly ILogger<RoomController> _logger;

    public RoomController(IFrontendService frontendService, ILogger<RoomController> logger)
    {
        _frontendService = frontendService ?? throw new ArgumentNullException(nameof(frontendService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("availability")]
    public async Task<IActionResult> GetAvailableRooms([FromBody] RoomAvailabilityQueryDTO query, 
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _frontendService.GetAvailableRoomsAsync(query, pageNumber, pageSize);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available rooms");
            return StatusCode(500, new { error = "An error occurred while retrieving available rooms" });
        }
    }

    [HttpGet("{roomId}")]
    public async Task<IActionResult> GetRoom(int roomId)
    {
        try
        {
            var result = await _frontendService.GetRoomAsync(roomId);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving room {RoomId}", roomId);
            return StatusCode(500, new { error = "An error occurred while retrieving room information" });
        }
    }

    [HttpGet("{roomId}/rate")]
    public async Task<IActionResult> GetRoomRate(int roomId, [FromQuery] DateTime checkIn, [FromQuery] DateTime checkOut)
    {
        try
        {
            var result = await _frontendService.GetRoomRateAsync(roomId, checkIn, checkOut);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating room rate for room {RoomId}", roomId);
            return StatusCode(500, new { error = "An error occurred while calculating room rate" });
        }
    }
}