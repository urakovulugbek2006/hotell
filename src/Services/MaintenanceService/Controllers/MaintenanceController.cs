using HotelOS.Shared.Models;
using MaintenanceService.DTOs;
using MaintenanceService.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _maintenanceService;
    private readonly IPriorityQueueService _priorityQueueService;
    private readonly ILogger<MaintenanceController> _logger;

    public MaintenanceController(
        IMaintenanceService maintenanceService,
        IPriorityQueueService priorityQueueService,
        ILogger<MaintenanceController> logger)
    {
        _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));
        _priorityQueueService = priorityQueueService ?? throw new ArgumentNullException(nameof(priorityQueueService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateMaintenanceRequestDTO request)
    {
        try
        {
            _logger.LogInformation("Creating maintenance request for room {RoomId} with priority {Priority}", 
                request.RoomId, request.Priority);

            var result = await _maintenanceService.CreateRequestAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            _logger.LogWarning("Failed to create maintenance request: {Error}", result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating maintenance request");
            return StatusCode(500, new { error = "An error occurred while creating the maintenance request" });
        }
    }

    [HttpPost("requests/{requestId}/assign")]
    public async Task<IActionResult> AssignRequest(int requestId, [FromBody] AssignMaintenanceRequestDTO request)
    {
        try
        {
            request.RequestId = requestId;
            var result = await _maintenanceService.AssignRequestAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning maintenance request {RequestId}", requestId);
            return StatusCode(500, new { error = "An error occurred while assigning the request" });
        }
    }

    [HttpPost("requests/{requestId}/start")]
    public async Task<IActionResult> StartWork(int requestId, [FromBody] StartWorkRequest request)
    {
        try
        {
            var result = await _maintenanceService.StartWorkAsync(requestId, request.TechnicianId);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting work on maintenance request {RequestId}", requestId);
            return StatusCode(500, new { error = "An error occurred while starting work" });
        }
    }

    [HttpPost("requests/{requestId}/complete")]
    public async Task<IActionResult> CompleteRequest(int requestId, [FromBody] CompleteMaintenanceRequestDTO request)
    {
        try
        {
            request.RequestId = requestId;
            var result = await _maintenanceService.CompleteRequestAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing maintenance request {RequestId}", requestId);
            return StatusCode(500, new { error = "An error occurred while completing the request" });
        }
    }

    [HttpPost("requests/{requestId}/auto-assign")]
    public async Task<IActionResult> AutoAssignRequest(int requestId)
    {
        try
        {
            var result = await _maintenanceService.AutoAssignRequestAsync(requestId);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-assigning maintenance request {RequestId}", requestId);
            return StatusCode(500, new { error = "An error occurred while auto-assigning the request" });
        }
    }

    [HttpPost("auto-assign")]
    public async Task<IActionResult> AutoAssignAllRequests()
    {
        try
        {
            await _maintenanceService.AutoAssignRequestsAsync();
            return Ok(new { message = "Auto-assignment completed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-assignment process");
            return StatusCode(500, new { error = "An error occurred during auto-assignment" });
        }
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests(
        [FromQuery] MaintenanceStatus? status = null,
        [FromQuery] MaintenancePriority? priority = null,
        [FromQuery] int? roomId = null,
        [FromQuery] int? technicianId = null)
    {
        try
        {
            IEnumerable<MaintenanceRequestDTO> requests;

            if (status.HasValue)
            {
                requests = await _maintenanceService.GetRequestsByStatusAsync(status.Value);
            }
            else if (priority.HasValue)
            {
                requests = await _maintenanceService.GetRequestsByPriorityAsync(priority.Value);
            }
            else if (roomId.HasValue)
            {
                requests = await _maintenanceService.GetRequestsByRoomAsync(roomId.Value);
            }
            else if (technicianId.HasValue)
            {
                requests = await _maintenanceService.GetRequestsByTechnicianAsync(technicianId.Value);
            }
            else
            {
                // Return pending requests by default
                requests = await _maintenanceService.GetRequestsByStatusAsync(MaintenanceStatus.Reported);
            }

            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching maintenance requests");
            return StatusCode(500, new { error = "An error occurred while fetching maintenance requests" });
        }
    }

    [HttpGet("requests/{requestId}")]
    public async Task<IActionResult> GetRequest(int requestId)
    {
        try
        {
            var request = await _maintenanceService.GetRequestAsync(requestId);
            
            if (request == null)
            {
                return NotFound(new { message = "Maintenance request not found" });
            }

            return Ok(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching maintenance request {RequestId}", requestId);
            return StatusCode(500, new { error = "An error occurred while fetching the maintenance request" });
        }
    }

    [HttpGet("requests/overdue")]
    public async Task<IActionResult> GetOverdueRequests()
    {
        try
        {
            var requests = await _maintenanceService.GetOverdueRequestsAsync();
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching overdue maintenance requests");
            return StatusCode(500, new { error = "An error occurred while fetching overdue requests" });
        }
    }

    [HttpGet("queue/status")]
    public async Task<IActionResult> GetQueueStatus()
    {
        try
        {
            var status = await _priorityQueueService.GetPriorityQueueSummaryAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching queue status");
            return StatusCode(500, new { error = "An error occurred while fetching queue status" });
        }
    }

    [HttpGet("queue/next")]
    public async Task<IActionResult> GetNextRequest()
    {
        try
        {
            var request = await _priorityQueueService.GetNextRequestAsync();
            
            if (request == null)
            {
                return NotFound(new { message = "No requests in queue" });
            }

            return Ok(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next request from queue");
            return StatusCode(500, new { error = "An error occurred while getting the next request" });
        }
    }

    [HttpPost("queue/rebalance")]
    public async Task<IActionResult> RebalanceQueue()
    {
        try
        {
            await _priorityQueueService.RebalanceQueueAsync();
            return Ok(new { message = "Queue rebalanced successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebalancing queue");
            return StatusCode(500, new { error = "An error occurred while rebalancing the queue" });
        }
    }

    [HttpGet("technicians/workloads")]
    public async Task<IActionResult> GetTechnicianWorkloads()
    {
        try
        {
            var workloads = await _maintenanceService.GetTechnicianWorkloadsAsync();
            return Ok(workloads);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching technician workloads");
            return StatusCode(500, new { error = "An error occurred while fetching technician workloads" });
        }
    }

    [HttpGet("technicians/{technicianId}/workload")]
    public async Task<IActionResult> GetTechnicianWorkload(int technicianId)
    {
        try
        {
            var workload = await _maintenanceService.GetTechnicianWorkloadAsync(technicianId);
            
            if (workload == null)
            {
                return NotFound(new { message = "Technician not found" });
            }

            return Ok(workload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workload for technician {TechnicianId}", technicianId);
            return StatusCode(500, new { error = "An error occurred while fetching technician workload" });
        }
    }

    [HttpGet("technicians/available")]
    public async Task<IActionResult> GetAvailableTechnicians()
    {
        try
        {
            var technicians = await _maintenanceService.GetAvailableTechniciansAsync();
            return Ok(technicians);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available technicians");
            return StatusCode(500, new { error = "An error occurred while fetching available technicians" });
        }
    }

    [HttpGet("reports/summary")]
    public async Task<IActionResult> GetMaintenanceSummary([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var summary = await _maintenanceService.GetMaintenanceSummaryAsync(startDate, endDate);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching maintenance summary");
            return StatusCode(500, new { error = "An error occurred while fetching maintenance summary" });
        }
    }
}

// Additional DTOs for controller endpoints
public class StartWorkRequest
{
    public int TechnicianId { get; set; }
}