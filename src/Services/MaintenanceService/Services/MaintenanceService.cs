using HotelOS.Shared.Events;
using HotelOS.Shared.Infrastructure;
using HotelOS.Shared.Models;
using MaintenanceService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceService.Services;

public class MaintenanceService : IMaintenanceService
{
    private readonly HotelDbContext _context;
    private readonly IMessageBroker _messageBroker;
    private readonly IPriorityQueueService _priorityQueueService;
    private readonly ILogger<MaintenanceService> _logger;

    public MaintenanceService(
        HotelDbContext context,
        IMessageBroker messageBroker,
        IPriorityQueueService priorityQueueService,
        ILogger<MaintenanceService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        _priorityQueueService = priorityQueueService ?? throw new ArgumentNullException(nameof(priorityQueueService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MaintenanceResponse> CreateRequestAsync(CreateMaintenanceRequestDTO request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Validate room exists
            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room == null)
            {
                return new MaintenanceResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Room not found" 
                };
            }

            // Create maintenance request
            var maintenanceRequest = new MaintenanceRequest
            {
                RoomId = request.RoomId,
                Description = request.Description,
                Priority = request.Priority,
                Status = MaintenanceStatus.Reported,
                ReportedAt = DateTime.UtcNow,
                ReportedBy = request.ReportedBy ?? "System",
                EstimatedCost = request.EstimatedCost
            };

            _context.MaintenanceRequests.Add(maintenanceRequest);
            await _context.SaveChangesAsync();

            // Update room status if critical issue
            if (request.Priority == MaintenancePriority.Critical || request.IsEmergency)
            {
                room.Status = RoomStatus.Maintenance;
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            // Add to priority queue
            var queuePosition = await _priorityQueueService.AddToQueueAsync(maintenanceRequest);
            var estimatedWaitTime = await _priorityQueueService.GetEstimatedWaitTimeAsync(maintenanceRequest.Id);

            // Publish event
            var requestedEvent = new MaintenanceRequestedEvent
            {
                RequestId = maintenanceRequest.Id,
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                Description = request.Description,
                Priority = request.Priority,
                ReportedTime = maintenanceRequest.ReportedAt,
                ReportedBy = request.ReportedBy ?? "System",
                EstimatedCost = request.EstimatedCost
            };

            await _messageBroker.PublishAsync(requestedEvent, EventTopics.MaintenanceRequested);

            _logger.LogInformation("Created maintenance request {RequestId} for room {RoomNumber} with priority {Priority}", 
                maintenanceRequest.Id, room.RoomNumber, request.Priority);

            var requestDTO = await GetRequestAsync(maintenanceRequest.Id);
            return new MaintenanceResponse
            {
                IsSuccess = true,
                Request = requestDTO,
                QueuePosition = queuePosition,
                EstimatedWaitTime = estimatedWaitTime
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating maintenance request for room {RoomId}", request.RoomId);
            throw;
        }
    }

    public async Task<MaintenanceResponse> AssignRequestAsync(AssignMaintenanceRequestDTO request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var maintenanceRequest = await _context.MaintenanceRequests
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == request.RequestId);

            var technician = await _context.Staff
                .FirstOrDefaultAsync(s => s.Id == request.TechnicianId && s.Role == StaffRole.Technician);

            if (maintenanceRequest == null || technician == null)
            {
                return new MaintenanceResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Request or technician not found" 
                };
            }

            if (maintenanceRequest.Status != MaintenanceStatus.Reported)
            {
                return new MaintenanceResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"Request cannot be assigned in current status: {maintenanceRequest.Status}" 
                };
            }

            // Check if technician is available
            var activeTasks = await _context.MaintenanceRequests
                .CountAsync(r => r.AssignedTechnicianId == request.TechnicianId && 
                                r.Status == MaintenanceStatus.InProgress);

            if (activeTasks >= 3) // Max 3 concurrent tasks
            {
                return new MaintenanceResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Technician already has maximum number of active tasks" 
                };
            }

            // Assign request
            maintenanceRequest.AssignedTechnicianId = request.TechnicianId;
            maintenanceRequest.AssignedAt = DateTime.UtcNow;
            maintenanceRequest.Status = MaintenanceStatus.Assigned;

            await _context.SaveChangesAsync();

            // Remove from priority queue
            await _priorityQueueService.RemoveFromQueueAsync(request.RequestId);

            await transaction.CommitAsync();

            // Publish event
            var assignedEvent = new MaintenanceAssignedEvent
            {
                RequestId = maintenanceRequest.Id,
                RoomId = maintenanceRequest.RoomId,
                RoomNumber = maintenanceRequest.Room.RoomNumber,
                TechnicianId = technician.Id,
                TechnicianName = technician.FullName,
                AssignedTime = maintenanceRequest.AssignedAt.Value,
                EstimatedDuration = request.EstimatedDuration ?? TimeSpan.FromHours(1),
                Priority = maintenanceRequest.Priority
            };

            await _messageBroker.PublishAsync(assignedEvent, EventTopics.MaintenanceAssigned);

            _logger.LogInformation("Assigned maintenance request {RequestId} to technician {TechnicianName}", 
                request.RequestId, technician.FullName);

            var requestDTO = await GetRequestAsync(maintenanceRequest.Id);
            return new MaintenanceResponse
            {
                IsSuccess = true,
                Request = requestDTO
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error assigning maintenance request {RequestId}", request.RequestId);
            throw;
        }
    }

    public async Task<MaintenanceResponse> StartWorkAsync(int requestId, int technicianId)
    {
        try
        {
            var request = await _context.MaintenanceRequests
                .Include(r => r.Room)
                .Include(r => r.AssignedTechnician)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                return new MaintenanceResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Request not found" 
                };
            }

            if (request.AssignedTechnicianId != technicianId)
            {
                return new MaintenanceResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Request is not assigned to this technician" 
                };
            }

            if (request.Status != MaintenanceStatus.Assigned)
            {
                return new MaintenanceResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"Cannot start work on request in status: {request.Status}" 
                };
            }

            request.Status = MaintenanceStatus.InProgress;
            await _context.SaveChangesAsync();

            // Publish event
            var startedEvent = new MaintenanceStartedEvent
            {
                RequestId = request.Id,
                RoomId = request.RoomId,
                RoomNumber = request.Room.RoomNumber,
                TechnicianId = request.AssignedTechnician!.Id,
                TechnicianName = request.AssignedTechnician.FullName,
                StartTime = DateTime.UtcNow
            };

            await _messageBroker.PublishAsync(startedEvent, EventTopics.MaintenanceStarted);

            _logger.LogInformation("Started work on maintenance request {RequestId} by technician {TechnicianName}", 
                requestId, request.AssignedTechnician.FullName);

            var requestDTO = await GetRequestAsync(request.Id);
            return new MaintenanceResponse { IsSuccess = true, Request = requestDTO };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting work on maintenance request {RequestId}", requestId);
            throw;
        }
    }

    public async Task<MaintenanceResponse> CompleteRequestAsync(CompleteMaintenanceRequestDTO request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var maintenanceRequest = await _context.MaintenanceRequests
                .Include(r => r.Room)
                .Include(r => r.AssignedTechnician)
                .FirstOrDefaultAsync(r => r.Id == request.RequestId);

            if (maintenanceRequest == null)
            {
                return new MaintenanceResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Request not found" 
                };
            }

            if (maintenanceRequest.AssignedTechnicianId != request.TechnicianId)
            {
                return new MaintenanceResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Request is not assigned to this technician" 
                };
            }

            if (maintenanceRequest.Status != MaintenanceStatus.InProgress)
            {
                return new MaintenanceResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"Cannot complete request in status: {maintenanceRequest.Status}" 
                };
            }

            // Complete the request
            maintenanceRequest.Status = MaintenanceStatus.Completed;
            maintenanceRequest.CompletedAt = DateTime.UtcNow;
            maintenanceRequest.ResolutionNotes = request.ResolutionNotes;
            maintenanceRequest.ActualCost = request.ActualCost;

            // Update room status if back in service
            if (request.RoomBackInService && maintenanceRequest.Room.Status == RoomStatus.Maintenance)
            {
                maintenanceRequest.Room.Status = RoomStatus.Dirty; // Needs cleaning after maintenance
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Publish event
            var completedEvent = new MaintenanceCompletedEvent
            {
                RequestId = maintenanceRequest.Id,
                RoomId = maintenanceRequest.RoomId,
                RoomNumber = maintenanceRequest.Room.RoomNumber,
                TechnicianId = maintenanceRequest.AssignedTechnician!.Id,
                TechnicianName = maintenanceRequest.AssignedTechnician.FullName,
                CompletedTime = maintenanceRequest.CompletedAt.Value,
                ActualDuration = maintenanceRequest.ResolutionTime ?? TimeSpan.Zero,
                ResolutionNotes = request.ResolutionNotes,
                ActualCost = request.ActualCost,
                RoomBackInService = request.RoomBackInService
            };

            await _messageBroker.PublishAsync(completedEvent, EventTopics.MaintenanceCompleted);

            _logger.LogInformation("Completed maintenance request {RequestId} by technician {TechnicianName}, room back in service: {BackInService}", 
                request.RequestId, maintenanceRequest.AssignedTechnician.FullName, request.RoomBackInService);

            var requestDTO = await GetRequestAsync(maintenanceRequest.Id);
            return new MaintenanceResponse { IsSuccess = true, Request = requestDTO };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error completing maintenance request {RequestId}", request.RequestId);
            throw;
        }
    }

    public async Task<MaintenanceResponse> AutoAssignRequestAsync(int requestId)
    {
        try
        {
            var bestTechnician = await GetBestTechnicianForRequestAsync(requestId);
            if (bestTechnician == null)
            {
                return new MaintenanceResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "No suitable technician available" 
                };
            }

            var assignRequest = new AssignMaintenanceRequestDTO
            {
                RequestId = requestId,
                TechnicianId = bestTechnician.Id,
                EstimatedDuration = TimeSpan.FromHours(1)
            };

            return await AssignRequestAsync(assignRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-assigning maintenance request {RequestId}", requestId);
            throw;
        }
    }

    public async Task AutoAssignRequestsAsync()
    {
        try
        {
            var unassignedRequests = await _context.MaintenanceRequests
                .Where(r => r.Status == MaintenanceStatus.Reported)
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.ReportedAt)
                .ToListAsync();

            var assignedCount = 0;
            
            foreach (var request in unassignedRequests)
            {
                var result = await AutoAssignRequestAsync(request.Id);
                if (result.IsSuccess)
                {
                    assignedCount++;
                }
            }

            _logger.LogInformation("Auto-assigned {Count} maintenance requests", assignedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-assignment process");
        }
    }

    // Query methods
    public async Task<IEnumerable<MaintenanceRequestDTO>> GetRequestsByStatusAsync(MaintenanceStatus status)
    {
        var requests = await _context.MaintenanceRequests
            .Include(r => r.Room)
            .Include(r => r.AssignedTechnician)
            .Where(r => r.Status == status)
            .OrderBy(r => r.ReportedAt)
            .ToListAsync();

        return requests.Select(MapToMaintenanceRequestDTO);
    }

    public async Task<IEnumerable<MaintenanceRequestDTO>> GetRequestsByPriorityAsync(MaintenancePriority priority)
    {
        var requests = await _context.MaintenanceRequests
            .Include(r => r.Room)
            .Include(r => r.AssignedTechnician)
            .Where(r => r.Priority == priority && r.Status != MaintenanceStatus.Completed)
            .OrderBy(r => r.ReportedAt)
            .ToListAsync();

        return requests.Select(MapToMaintenanceRequestDTO);
    }

    public async Task<IEnumerable<MaintenanceRequestDTO>> GetRequestsByTechnicianAsync(int technicianId)
    {
        var requests = await _context.MaintenanceRequests
            .Include(r => r.Room)
            .Include(r => r.AssignedTechnician)
            .Where(r => r.AssignedTechnicianId == technicianId)
            .OrderByDescending(r => r.ReportedAt)
            .ToListAsync();

        return requests.Select(MapToMaintenanceRequestDTO);
    }

    public async Task<IEnumerable<MaintenanceRequestDTO>> GetRequestsByRoomAsync(int roomId)
    {
        var requests = await _context.MaintenanceRequests
            .Include(r => r.Room)
            .Include(r => r.AssignedTechnician)
            .Where(r => r.RoomId == roomId)
            .OrderByDescending(r => r.ReportedAt)
            .ToListAsync();

        return requests.Select(MapToMaintenanceRequestDTO);
    }

    public async Task<IEnumerable<MaintenanceRequestDTO>> GetOverdueRequestsAsync()
    {
        var now = DateTime.UtcNow;
        
        var requests = await _context.MaintenanceRequests
            .Include(r => r.Room)
            .Include(r => r.AssignedTechnician)
            .Where(r => r.Status != MaintenanceStatus.Completed && r.Status != MaintenanceStatus.Cancelled)
            .ToListAsync();

        var overdueRequests = requests.Where(r => 
            r.ReportedAt.Add(r.Priority.GetMaxWaitTime()) < now).ToList();

        return overdueRequests.Select(MapToMaintenanceRequestDTO);
    }

    public async Task<MaintenanceRequestDTO?> GetRequestAsync(int requestId)
    {
        var request = await _context.MaintenanceRequests
            .Include(r => r.Room)
            .Include(r => r.AssignedTechnician)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        return request != null ? MapToMaintenanceRequestDTO(request) : null;
    }

    public async Task<IEnumerable<TechnicianWorkloadDTO>> GetTechnicianWorkloadsAsync()
    {
        var technicians = await _context.Staff
            .Where(s => s.Role == StaffRole.Technician)
            .ToListAsync();

        var workloads = new List<TechnicianWorkloadDTO>();
        
        foreach (var technician in technicians)
        {
            var activeRequests = await GetRequestsByTechnicianAsync(technician.Id);
            var activeRequestsList = activeRequests.Where(r => r.Status != MaintenanceStatus.Completed).ToList();
            
            workloads.Add(new TechnicianWorkloadDTO
            {
                TechnicianId = technician.Id,
                TechnicianName = technician.FullName,
                Status = technician.Status,
                ActiveRequests = activeRequestsList.Count,
                CompletedRequests = 0, // Would need proper tracking
                AverageResolutionTime = TimeSpan.FromMinutes(45),
                CurrentRequests = activeRequestsList,
                IsAvailable = technician.Status == StaffStatus.Active && activeRequestsList.Count < 3,
                Specialty = MaintenanceSpecialty.General // Would be configured per technician
            });
        }

        return workloads;
    }

    public async Task<TechnicianWorkloadDTO?> GetTechnicianWorkloadAsync(int technicianId)
    {
        var workloads = await GetTechnicianWorkloadsAsync();
        return workloads.FirstOrDefault(w => w.TechnicianId == technicianId);
    }

    public async Task<IEnumerable<Staff>> GetAvailableTechniciansAsync()
    {
        return await _context.Staff
            .Where(s => s.Role == StaffRole.Technician && s.Status == StaffStatus.Active)
            .ToListAsync();
    }

    public async Task<Staff?> GetBestTechnicianForRequestAsync(int requestId)
    {
        var request = await _context.MaintenanceRequests.FindAsync(requestId);
        if (request == null) return null;

        var availableTechnicians = await GetAvailableTechniciansAsync();
        var workloads = await GetTechnicianWorkloadsAsync();
        
        // Find technician with lowest workload
        var bestTechnician = workloads
            .Where(w => w.IsAvailable)
            .OrderBy(w => w.ActiveRequests)
            .ThenBy(w => w.AverageResolutionTime)
            .FirstOrDefault();

        if (bestTechnician == null) return null;
        
        return availableTechnicians.FirstOrDefault(t => t.Id == bestTechnician.TechnicianId);
    }

    public async Task<MaintenanceSummaryDTO> GetMaintenanceSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.MaintenanceRequests.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(r => r.ReportedAt >= startDate.Value);
            
        if (endDate.HasValue)
            query = query.Where(r => r.ReportedAt <= endDate.Value);

        var requests = await query.ToListAsync();
        var overdueRequests = await GetOverdueRequestsAsync();

        return new MaintenanceSummaryDTO
        {
            TotalRequests = requests.Count,
            PendingRequests = requests.Count(r => r.Status == MaintenanceStatus.Reported),
            AssignedRequests = requests.Count(r => r.Status == MaintenanceStatus.Assigned),
            InProgressRequests = requests.Count(r => r.Status == MaintenanceStatus.InProgress),
            CompletedRequests = requests.Count(r => r.Status == MaintenanceStatus.Completed),
            CancelledRequests = requests.Count(r => r.Status == MaintenanceStatus.Cancelled),
            CriticalRequests = requests.Count(r => r.Priority == MaintenancePriority.Critical),
            OverdueRequests = overdueRequests.Count(),
            TotalCost = requests.Where(r => r.ActualCost.HasValue).Sum(r => r.ActualCost!.Value),
            AverageResponseTime = TimeSpan.FromMinutes(30), // Default
            AverageResolutionTime = TimeSpan.FromMinutes(45), // Default
            RequestsByPriority = requests.GroupBy(r => r.Priority).ToDictionary(g => g.Key, g => g.Count()),
            RequestsByStatus = requests.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<PriorityQueueStatusDTO> GetQueueStatusAsync()
    {
        return await _priorityQueueService.GetPriorityQueueSummaryAsync();
    }

    // Placeholder methods for additional functionality
    public async Task<MaintenanceResponse> UpdateStatusAsync(UpdateMaintenanceStatusDTO request)
    {
        // Implementation for generic status updates
        return new MaintenanceResponse { IsSuccess = true };
    }

    public async Task<MaintenanceResponse> CancelRequestAsync(int requestId, string reason)
    {
        // Implementation for canceling requests
        return new MaintenanceResponse { IsSuccess = true };
    }

    public async Task<MaintenanceResponse> PauseWorkAsync(int requestId, int technicianId, string reason)
    {
        // Implementation for pausing work
        return new MaintenanceResponse { IsSuccess = true };
    }

    public async Task<MaintenanceResponse> ResumeWorkAsync(int requestId, int technicianId)
    {
        // Implementation for resuming work
        return new MaintenanceResponse { IsSuccess = true };
    }

    // Helper method
    private static MaintenanceRequestDTO MapToMaintenanceRequestDTO(MaintenanceRequest request)
    {
        return new MaintenanceRequestDTO
        {
            Id = request.Id,
            RoomId = request.RoomId,
            RoomNumber = request.Room.RoomNumber,
            Description = request.Description,
            Priority = request.Priority,
            Status = request.Status,
            ReportedAt = request.ReportedAt,
            AssignedAt = request.AssignedAt,
            CompletedAt = request.CompletedAt,
            AssignedTechnicianId = request.AssignedTechnicianId,
            AssignedTechnicianName = request.AssignedTechnician?.FullName,
            ReportedBy = request.ReportedBy,
            ResolutionNotes = request.ResolutionNotes,
            EstimatedCost = request.EstimatedCost,
            ActualCost = request.ActualCost,
            ResponseTime = request.ResponseTime,
            ResolutionTime = request.ResolutionTime,
            IsOverdue = request.ReportedAt.Add(request.Priority.GetMaxWaitTime()) < DateTime.UtcNow && 
                       request.Status != MaintenanceStatus.Completed
        };
    }
}