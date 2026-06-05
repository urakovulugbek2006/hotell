using FrontendService.DTOs;
using FrontendService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontendService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MaintenanceController : ControllerBase
{
    private readonly IFrontendService _frontendService;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(IFrontendService frontendService, ILogger<MaintenanceController> logger)
    {
        _frontendService = frontendService ?? throw new ArgumentNullException(nameof(frontendService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateMaintenanceRequestDTO request)
    {
        try
        {
            var result = await _frontendService.CreateMaintenanceRequestAsync(request);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating maintenance request");
            return StatusCode(500, new { error = "An error occurred while creating the maintenance request" });
        }
    }

    [HttpGet("requests/{requestId}")]
    public async Task<IActionResult> GetRequest(int requestId)
    {
        try
        {
            var result = await _frontendService.GetMaintenanceRequestAsync(requestId);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving maintenance request {RequestId}", requestId);
            return StatusCode(500, new { error = "An error occurred while retrieving the maintenance request" });
        }
    }

    [HttpGet("requests/room/{roomId}")]
    public async Task<IActionResult> GetRequestsByRoom(int roomId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _frontendService.GetMaintenanceRequestsByRoomAsync(roomId, pageNumber, pageSize);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving maintenance requests for room {RoomId}", roomId);
            return StatusCode(500, new { error = "An error occurred while retrieving maintenance requests" });
        }
    }
}