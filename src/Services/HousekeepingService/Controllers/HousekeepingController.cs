using HousekeepingService.DTOs;
using HousekeepingService.Services;
using HotelOS.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace HousekeepingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HousekeepingController : ControllerBase
{
    private readonly IHousekeepingService _housekeepingService;
    private readonly ILogger<HousekeepingController> _logger;

    public HousekeepingController(IHousekeepingService housekeepingService, ILogger<HousekeepingController> logger)
    {
        _housekeepingService = housekeepingService ?? throw new ArgumentNullException(nameof(housekeepingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("start-cleaning")]
    public async Task<IActionResult> StartCleaning([FromBody] StartCleaningRequest request)
    {
        try
        {
            _logger.LogInformation("Starting cleaning for room {RoomId} by housekeeper {HousekeeperId}", 
                request.RoomId, request.HousekeeperId);

            var result = await _housekeepingService.StartCleaningAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            _logger.LogWarning("Failed to start cleaning: {Error}", result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting cleaning for room {RoomId}", request.RoomId);
            return StatusCode(500, new { error = "An error occurred while starting cleaning" });
        }
    }

    [HttpPost("complete-cleaning")]
    public async Task<IActionResult> CompleteCleaning([FromBody] CompleteCleaningRequest request)
    {
        try
        {
            _logger.LogInformation("Completing cleaning for room {RoomId} by housekeeper {HousekeeperId}", 
                request.RoomId, request.HousekeeperId);

            var result = await _housekeepingService.CompleteCleaningAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            _logger.LogWarning("Failed to complete cleaning: {Error}", result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing cleaning for room {RoomId}", request.RoomId);
            return StatusCode(500, new { error = "An error occurred while completing cleaning" });
        }
    }

    [HttpPost("assign-task")]
    public async Task<IActionResult> AssignCleaningTask([FromBody] AssignCleaningTaskRequest request)
    {
        try
        {
            var result = await _housekeepingService.AssignCleaningTaskAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning cleaning task for room {RoomId}", request.RoomId);
            return StatusCode(500, new { error = "An error occurred while assigning cleaning task" });
        }
    }

    [HttpPut("room-status")]
    public async Task<IActionResult> UpdateRoomStatus([FromBody] UpdateRoomStatusRequest request)
    {
        try
        {
            var result = await _housekeepingService.UpdateRoomStatusAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room status for room {RoomId}", request.RoomId);
            return StatusCode(500, new { error = "An error occurred while updating room status" });
        }
    }

    [HttpGet("tasks/pending")]
    public async Task<IActionResult> GetPendingTasks()
    {
        try
        {
            var tasks = await _housekeepingService.GetPendingTasksAsync();
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending tasks");
            return StatusCode(500, new { error = "An error occurred while fetching pending tasks" });
        }
    }

    [HttpGet("tasks/overdue")]
    public async Task<IActionResult> GetOverdueTasks()
    {
        try
        {
            var tasks = await _housekeepingService.GetOverdueTasksAsync();
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching overdue tasks");
            return StatusCode(500, new { error = "An error occurred while fetching overdue tasks" });
        }
    }

    [HttpGet("tasks/housekeeper/{housekeeperId}")]
    public async Task<IActionResult> GetTasksByHousekeeper(int housekeeperId)
    {
        try
        {
            var tasks = await _housekeepingService.GetTasksByHousekeeperAsync(housekeeperId);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tasks for housekeeper {HousekeeperId}", housekeeperId);
            return StatusCode(500, new { error = "An error occurred while fetching housekeeper tasks" });
        }
    }

    [HttpGet("tasks/room/{roomId}/active")]
    public async Task<IActionResult> GetActiveTaskForRoom(int roomId)
    {
        try
        {
            var task = await _housekeepingService.GetActiveTaskForRoomAsync(roomId);
            
            if (task == null)
            {
                return NotFound(new { message = "No active cleaning task found for this room" });
            }

            return Ok(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active task for room {RoomId}", roomId);
            return StatusCode(500, new { error = "An error occurred while fetching room task" });
        }
    }

    [HttpGet("rooms/status/{status}")]
    public async Task<IActionResult> GetRoomsByStatus(RoomStatus status)
    {
        try
        {
            var rooms = await _housekeepingService.GetRoomsByStatusAsync(status);
            return Ok(rooms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching rooms by status {Status}", status);
            return StatusCode(500, new { error = "An error occurred while fetching rooms" });
        }
    }

    [HttpGet("rooms/summary")]
    public async Task<IActionResult> GetRoomStatusSummary()
    {
        try
        {
            var summary = await _housekeepingService.GetRoomStatusSummaryAsync();
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching room status summary");
            return StatusCode(500, new { error = "An error occurred while fetching room status summary" });
        }
    }

    [HttpGet("housekeepers/workloads")]
    public async Task<IActionResult> GetHousekeeperWorkloads()
    {
        try
        {
            var workloads = await _housekeepingService.GetHousekeeperWorkloadsAsync();
            return Ok(workloads);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching housekeeper workloads");
            return StatusCode(500, new { error = "An error occurred while fetching housekeeper workloads" });
        }
    }

    [HttpGet("housekeepers/{housekeeperId}/workload")]
    public async Task<IActionResult> GetHousekeeperWorkload(int housekeeperId)
    {
        try
        {
            var workload = await _housekeepingService.GetHousekeeperWorkloadAsync(housekeeperId);
            
            if (workload == null)
            {
                return NotFound(new { message = "Housekeeper not found" });
            }

            return Ok(workload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workload for housekeeper {HousekeeperId}", housekeeperId);
            return StatusCode(500, new { error = "An error occurred while fetching housekeeper workload" });
        }
    }

    [HttpGet("housekeepers/available")]
    public async Task<IActionResult> GetAvailableHousekeepers()
    {
        try
        {
            var housekeepers = await _housekeepingService.GetAvailableHousekeepersAsync();
            return Ok(housekeepers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available housekeepers");
            return StatusCode(500, new { error = "An error occurred while fetching available housekeepers" });
        }
    }

    [HttpGet("rooms/{roomId}")]
    public async Task<IActionResult> GetRoom(int roomId)
    {
        try
        {
            var room = await _housekeepingService.GetRoomAsync(roomId);
            
            if (room == null)
            {
                return NotFound(new { message = "Room not found" });
            }

            return Ok(room);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching room {RoomId}", roomId);
            return StatusCode(500, new { error = "An error occurred while fetching room information" });
        }
    }
}