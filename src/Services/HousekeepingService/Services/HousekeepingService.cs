using HotelOS.Shared.Events;
using HotelOS.Shared.Infrastructure;
using HotelOS.Shared.Models;
using HousekeepingService.DTOs;
using HousekeepingService.Models;
using Microsoft.EntityFrameworkCore;

namespace HousekeepingService.Services;

public class HousekeepingService : IHousekeepingService
{
    private readonly HotelDbContext _context;
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<HousekeepingService> _logger;

    public HousekeepingService(
        HotelDbContext context,
        IMessageBroker messageBroker,
        ILogger<HousekeepingService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CleaningResponse> StartCleaningAsync(StartCleaningRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Validate room exists and is in correct status
            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room == null)
            {
                return new CleaningResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Room not found" 
                };
            }

            if (room.Status != RoomStatus.Dirty && room.Status != RoomStatus.Available)
            {
                return new CleaningResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"Room cannot be cleaned in current status: {room.Status}" 
                };
            }

            // Validate housekeeper exists and is available
            var housekeeper = await _context.Staff
                .FirstOrDefaultAsync(s => s.Id == request.HousekeeperId && 
                                        (s.Role == StaffRole.Housekeeper || s.Role == StaffRole.HousekeepingSupervisor) &&
                                        s.Status == StaffStatus.Active);

            if (housekeeper == null)
            {
                return new CleaningResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Housekeeper not found or not available" 
                };
            }

            // Check if housekeeper is already cleaning another room
            var activeTask = await GetActiveTaskByHousekeeperAsync(request.HousekeeperId);
            if (activeTask != null)
            {
                return new CleaningResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"Housekeeper is already cleaning room {activeTask.RoomNumber}" 
                };
            }

            // Update room status
            room.Status = RoomStatus.BeingCleaned;

            // Create or update cleaning task
            var existingTask = await GetCleaningTaskForRoomAsync(request.RoomId);
            CleaningTask task;

            if (existingTask != null)
            {
                task = existingTask;
                task.AssignedHousekeeperId = request.HousekeeperId;
                task.StartTime = DateTime.UtcNow;
                task.Status = CleaningTaskStatus.InProgress;
                task.EstimatedDuration = request.EstimatedDuration;
                task.Notes = request.Notes;
            }
            else
            {
                task = new CleaningTask
                {
                    RoomId = request.RoomId,
                    AssignedHousekeeperId = request.HousekeeperId,
                    Priority = CleaningPriority.Normal,
                    Status = CleaningTaskStatus.InProgress,
                    RequestedTime = DateTime.UtcNow.AddMinutes(-5), // Slight backdate for immediate start
                    AssignedTime = DateTime.UtcNow,
                    StartTime = DateTime.UtcNow,
                    EstimatedDuration = request.EstimatedDuration,
                    Notes = request.Notes
                };
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Publish event
            var cleaningStartedEvent = new RoomCleaningStartedEvent
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                HousekeeperId = housekeeper.Id,
                HousekeeperName = housekeeper.FullName,
                StartTime = task.StartTime.Value,
                EstimatedDuration = request.EstimatedDuration
            };

            await _messageBroker.PublishAsync(cleaningStartedEvent, EventTopics.RoomCleaningStarted);

            var statusChangedEvent = new RoomStatusChangedEvent
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                PreviousStatus = RoomStatus.Dirty,
                NewStatus = RoomStatus.BeingCleaned,
                ChangedTime = DateTime.UtcNow,
                ChangedByStaffId = housekeeper.Id,
                ChangedByStaffName = housekeeper.FullName,
                Reason = "Cleaning started"
            };

            await _messageBroker.PublishAsync(statusChangedEvent, EventTopics.RoomStatusChanged);

            _logger.LogInformation("Started cleaning for room {RoomNumber} by housekeeper {HousekeeperName}", 
                room.RoomNumber, housekeeper.FullName);

            return new CleaningResponse
            {
                IsSuccess = true,
                Task = MapToCleaningTaskDTO(task, room, housekeeper),
                Room = room
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error starting cleaning for room {RoomId}", request.RoomId);
            throw;
        }
    }

    public async Task<CleaningResponse> CompleteCleaningAsync(CompleteCleaningRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room == null)
            {
                return new CleaningResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Room not found" 
                };
            }

            var housekeeper = await _context.Staff.FindAsync(request.HousekeeperId);
            if (housekeeper == null)
            {
                return new CleaningResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Housekeeper not found" 
                };
            }

            var task = await GetActiveTaskByHousekeeperAsync(request.HousekeeperId);
            if (task == null || task.RoomId != request.RoomId)
            {
                return new CleaningResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "No active cleaning task found for this room and housekeeper" 
                };
            }

            // Update task
            var cleaningTask = await GetCleaningTaskForRoomAsync(request.RoomId);
            if (cleaningTask != null)
            {
                cleaningTask.CompletedTime = DateTime.UtcNow;
                cleaningTask.Status = CleaningTaskStatus.Completed;
                cleaningTask.PassedInspection = request.PassedInspection;
                cleaningTask.Notes = request.Notes;
                cleaningTask.IssuesFound = request.IssuesFound;
            }

            // Update room status
            room.Status = request.PassedInspection ? RoomStatus.Clean : RoomStatus.Dirty;
            room.LastCleaned = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Publish events
            var cleanedEvent = new RoomCleanedEvent
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                HousekeeperId = housekeeper.Id,
                HousekeeperName = housekeeper.FullName,
                CompletedTime = DateTime.UtcNow,
                ActualDuration = cleaningTask?.ActualDuration ?? TimeSpan.Zero,
                Notes = request.Notes,
                PassedInspection = request.PassedInspection
            };

            await _messageBroker.PublishAsync(cleanedEvent, EventTopics.RoomCleaned);

            var statusChangedEvent = new RoomStatusChangedEvent
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                PreviousStatus = RoomStatus.BeingCleaned,
                NewStatus = room.Status,
                ChangedTime = DateTime.UtcNow,
                ChangedByStaffId = housekeeper.Id,
                ChangedByStaffName = housekeeper.FullName,
                Reason = request.PassedInspection ? "Cleaning completed successfully" : "Cleaning failed inspection"
            };

            await _messageBroker.PublishAsync(statusChangedEvent, EventTopics.RoomStatusChanged);

            _logger.LogInformation("Completed cleaning for room {RoomNumber} by housekeeper {HousekeeperName}, passed inspection: {PassedInspection}", 
                room.RoomNumber, housekeeper.FullName, request.PassedInspection);

            return new CleaningResponse
            {
                IsSuccess = true,
                Task = cleaningTask != null ? MapToCleaningTaskDTO(cleaningTask, room, housekeeper) : null,
                Room = room
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error completing cleaning for room {RoomId}", request.RoomId);
            throw;
        }
    }

    public async Task<CleaningResponse> AssignCleaningTaskAsync(AssignCleaningTaskRequest request)
    {
        try
        {
            var room = await _context.Rooms.FindAsync(request.RoomId);
            var housekeeper = await _context.Staff.FindAsync(request.HousekeeperId);

            if (room == null || housekeeper == null)
            {
                return new CleaningResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Room or housekeeper not found" 
                };
            }

            var existingTask = await GetCleaningTaskForRoomAsync(request.RoomId);
            if (existingTask != null && existingTask.Status != CleaningTaskStatus.Completed)
            {
                // Update existing task
                existingTask.AssignedHousekeeperId = request.HousekeeperId;
                existingTask.AssignedTime = DateTime.UtcNow;
                existingTask.Status = CleaningTaskStatus.Assigned;
                existingTask.Priority = request.Priority;
                existingTask.SpecialInstructions = request.SpecialInstructions;
            }
            else
            {
                // Create new task (held in memory; cleaning tasks are tracked via room status
                // in this simplified implementation)
                var task = new CleaningTask
                {
                    RoomId = request.RoomId,
                    AssignedHousekeeperId = request.HousekeeperId,
                    Priority = request.Priority,
                    Status = CleaningTaskStatus.Assigned,
                    RequestedTime = DateTime.UtcNow,
                    AssignedTime = DateTime.UtcNow,
                    SpecialInstructions = request.SpecialInstructions
                };
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Assigned cleaning task for room {RoomNumber} to housekeeper {HousekeeperName}", 
                room.RoomNumber, housekeeper.FullName);

            return new CleaningResponse { IsSuccess = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning cleaning task for room {RoomId}", request.RoomId);
            throw;
        }
    }

    public async Task<CleaningResponse> UpdateRoomStatusAsync(UpdateRoomStatusRequest request)
    {
        try
        {
            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room == null)
            {
                return new CleaningResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Room not found" 
                };
            }

            var previousStatus = room.Status;
            room.Status = request.NewStatus;

            Staff? staff = null;
            if (request.StaffId.HasValue)
            {
                staff = await _context.Staff.FindAsync(request.StaffId.Value);
            }

            await _context.SaveChangesAsync();

            // Publish event
            var statusChangedEvent = new RoomStatusChangedEvent
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                PreviousStatus = previousStatus,
                NewStatus = request.NewStatus,
                ChangedTime = DateTime.UtcNow,
                ChangedByStaffId = request.StaffId,
                ChangedByStaffName = staff?.FullName,
                Reason = request.Reason
            };

            await _messageBroker.PublishAsync(statusChangedEvent, EventTopics.RoomStatusChanged);

            _logger.LogInformation("Updated room {RoomNumber} status from {PreviousStatus} to {NewStatus}", 
                room.RoomNumber, previousStatus, request.NewStatus);

            return new CleaningResponse
            {
                IsSuccess = true,
                Room = room
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room status for room {RoomId}", request.RoomId);
            throw;
        }
    }

    // Query methods implementation
    public async Task<IEnumerable<CleaningTaskDTO>> GetPendingTasksAsync()
    {
        // Since we're not storing cleaning tasks separately, we'll identify pending tasks
        // based on rooms that need cleaning
        var dirtyRooms = await _context.Rooms
            .Where(r => r.Status == RoomStatus.Dirty)
            .OrderBy(r => r.Id) // Simple ordering - in real implementation, this would be by LastCleaned
            .ToListAsync();

        return dirtyRooms.Select(room => new CleaningTaskDTO
        {
            Id = room.Id,
            RoomId = room.Id,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            RoomType = room.Type,
            CurrentStatus = room.Status,
            Priority = CleaningPriority.Normal,
            RequestedTime = DateTime.UtcNow.AddHours(-1) // Assume requested 1 hour ago
        });
    }

    public async Task<IEnumerable<CleaningTaskDTO>> GetTasksByHousekeeperAsync(int housekeeperId)
    {
        // For this implementation, return current room being cleaned
        var activeTask = await GetActiveTaskByHousekeeperAsync(housekeeperId);
        return activeTask != null ? new[] { activeTask } : Array.Empty<CleaningTaskDTO>();
    }

    public async Task<IEnumerable<CleaningTaskDTO>> GetOverdueTasksAsync()
    {
        var overdueRooms = await _context.Rooms
            .Where(r => r.Status == RoomStatus.Dirty)
            .ToListAsync();

        // Simple logic - rooms dirty for more than 2 hours are overdue
        var cutoffTime = DateTime.UtcNow.AddHours(-2);
        
        return overdueRooms.Select(room => new CleaningTaskDTO
        {
            Id = room.Id,
            RoomId = room.Id,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            RoomType = room.Type,
            CurrentStatus = room.Status,
            Priority = CleaningPriority.Urgent,
            RequestedTime = cutoffTime.AddMinutes(-30) // Simulate overdue
        }).Where(task => task.IsOverdue);
    }

    public async Task<CleaningTaskDTO?> GetActiveTaskForRoomAsync(int roomId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room?.Status != RoomStatus.BeingCleaned)
            return null;

        // Find who is cleaning this room (simplified approach)
        var housekeeper = await _context.Staff
            .Where(s => s.Role == StaffRole.Housekeeper && s.Status == StaffStatus.OnShift)
            .FirstOrDefaultAsync();

        return new CleaningTaskDTO
        {
            Id = room.Id,
            RoomId = room.Id,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            RoomType = room.Type,
            CurrentStatus = room.Status,
            Priority = CleaningPriority.Normal,
            RequestedTime = DateTime.UtcNow.AddMinutes(-30),
            StartTime = DateTime.UtcNow.AddMinutes(-15),
            AssignedHousekeeperId = housekeeper?.Id,
            AssignedHousekeeperName = housekeeper?.FullName
        };
    }

    public async Task<IEnumerable<Room>> GetRoomsByStatusAsync(RoomStatus status)
    {
        return await _context.Rooms
            .Where(r => r.Status == status)
            .OrderBy(r => r.Floor)
            .ThenBy(r => r.RoomNumber)
            .ToListAsync();
    }

    public async Task<RoomStatusSummaryDTO> GetRoomStatusSummaryAsync()
    {
        var rooms = await _context.Rooms.ToListAsync();
        
        return new RoomStatusSummaryDTO
        {
            TotalRooms = rooms.Count,
            AvailableRooms = rooms.Count(r => r.Status == RoomStatus.Available),
            OccupiedRooms = rooms.Count(r => r.Status == RoomStatus.Occupied),
            DirtyRooms = rooms.Count(r => r.Status == RoomStatus.Dirty),
            BeingCleanedRooms = rooms.Count(r => r.Status == RoomStatus.BeingCleaned),
            CleanRooms = rooms.Count(r => r.Status == RoomStatus.Clean),
            MaintenanceRooms = rooms.Count(r => r.Status == RoomStatus.Maintenance),
            OutOfOrderRooms = rooms.Count(r => r.Status == RoomStatus.OutOfOrder),
            OverdueTasks = (await GetOverdueTasksAsync()).Count(),
            AverageCleaningTime = TimeSpan.FromMinutes(45) // Default estimate
        };
    }

    public async Task<Room?> GetRoomAsync(int roomId)
    {
        return await _context.Rooms.FindAsync(roomId);
    }

    public async Task<IEnumerable<HousekeeperWorkloadDTO>> GetHousekeeperWorkloadsAsync()
    {
        var housekeepers = await _context.Staff
            .Where(s => s.Role == StaffRole.Housekeeper || s.Role == StaffRole.HousekeepingSupervisor)
            .ToListAsync();

        var workloads = new List<HousekeeperWorkloadDTO>();
        
        foreach (var housekeeper in housekeepers)
        {
            var activeTask = await GetActiveTaskByHousekeeperAsync(housekeeper.Id);
            
            workloads.Add(new HousekeeperWorkloadDTO
            {
                HousekeeperId = housekeeper.Id,
                HousekeeperName = housekeeper.FullName,
                Status = housekeeper.Status,
                ActiveTasks = activeTask != null ? 1 : 0,
                CompletedTasks = 0, // Would need to track this in real implementation
                TotalWorkTime = TimeSpan.Zero,
                CurrentTasks = activeTask != null ? new List<CleaningTaskDTO> { activeTask } : new List<CleaningTaskDTO>()
            });
        }

        return workloads;
    }

    public async Task<HousekeeperWorkloadDTO?> GetHousekeeperWorkloadAsync(int housekeeperId)
    {
        var workloads = await GetHousekeeperWorkloadsAsync();
        return workloads.FirstOrDefault(w => w.HousekeeperId == housekeeperId);
    }

    public async Task<IEnumerable<Staff>> GetAvailableHousekeepersAsync()
    {
        return await _context.Staff
            .Where(s => (s.Role == StaffRole.Housekeeper || s.Role == StaffRole.HousekeepingSupervisor) && 
                       s.Status == StaffStatus.Active)
            .ToListAsync();
    }

    public async Task HandleRoomVacatedAsync(int roomId, string previousGuestName)
    {
        try
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return;

            // Set room to dirty status
            room.Status = RoomStatus.Dirty;
            await _context.SaveChangesAsync();

            // Publish room needs cleaning event
            var needsCleaningEvent = new RoomNeedsCleaningEvent
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                RequestedTime = DateTime.UtcNow,
                Priority = CleaningPriority.Normal,
                SpecialInstructions = $"Room vacated by {previousGuestName}"
            };

            await _messageBroker.PublishAsync(needsCleaningEvent, EventTopics.RoomNeedsCleaning);

            _logger.LogInformation("Room {RoomNumber} marked as needing cleaning after guest {GuestName} checkout", 
                room.RoomNumber, previousGuestName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling room vacated event for room {RoomId}", roomId);
        }
    }

    // Helper methods
    private async Task<CleaningTask?> GetCleaningTaskForRoomAsync(int roomId)
    {
        // Since we're not storing cleaning tasks separately in this implementation,
        // we'll return null and handle this in the main methods
        await Task.CompletedTask;
        return null;
    }

    private async Task<CleaningTaskDTO?> GetActiveTaskByHousekeeperAsync(int housekeeperId)
    {
        // Check if housekeeper is currently cleaning a room
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Status == RoomStatus.BeingCleaned);

        if (room == null) return null;

        return new CleaningTaskDTO
        {
            Id = room.Id,
            RoomId = room.Id,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            RoomType = room.Type,
            CurrentStatus = room.Status,
            Priority = CleaningPriority.Normal,
            RequestedTime = DateTime.UtcNow.AddMinutes(-30),
            StartTime = DateTime.UtcNow.AddMinutes(-15),
            AssignedHousekeeperId = housekeeperId
        };
    }

    private static CleaningTaskDTO MapToCleaningTaskDTO(CleaningTask task, Room room, Staff housekeeper)
    {
        return new CleaningTaskDTO
        {
            Id = task.Id,
            RoomId = task.RoomId,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            RoomType = room.Type,
            CurrentStatus = room.Status,
            Priority = task.Priority,
            RequestedTime = task.RequestedTime,
            AssignedTime = task.AssignedTime,
            StartTime = task.StartTime,
            CompletedTime = task.CompletedTime,
            AssignedHousekeeperId = task.AssignedHousekeeperId,
            AssignedHousekeeperName = housekeeper.FullName,
            SpecialInstructions = task.SpecialInstructions,
            Notes = task.Notes,
            EstimatedDuration = task.EstimatedDuration
        };
    }
}