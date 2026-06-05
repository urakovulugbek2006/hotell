using HotelOS.Shared.Infrastructure;
using HotelOS.Shared.Models;
using MaintenanceService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceService.Services;

public class PriorityQueueService : IPriorityQueueService
{
    private readonly HotelDbContext _context;
    private readonly ILogger<PriorityQueueService> _logger;
    private static readonly SortedList<int, PriorityQueueItem> _priorityQueue = new(new DescendingComparer());
    private static readonly object _queueLock = new object();

    public PriorityQueueService(HotelDbContext context, ILogger<PriorityQueueService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> AddToQueueAsync(MaintenanceRequest request)
    {
        try
        {
            var specialty = DetermineRequiredSpecialty(request.Description);
            var isEmergency = request.Priority == MaintenancePriority.Critical || 
                             request.Description.Contains("emergency", StringComparison.OrdinalIgnoreCase);

            var queueItem = new PriorityQueueItem
            {
                RequestId = request.Id,
                Priority = request.Priority,
                QueuedAt = DateTime.UtcNow,
                RequiredSpecialty = specialty,
                IsEmergency = isEmergency
            };

            lock (_queueLock)
            {
                // Remove if already exists (for rebalancing)
                var existingKey = _priorityQueue.FirstOrDefault(kvp => kvp.Value.RequestId == request.Id).Key;
                if (existingKey != 0)
                {
                    _priorityQueue.Remove(existingKey);
                }

                // Add with priority score as key, ensuring uniqueness
                var key = queueItem.PriorityScore;
                while (_priorityQueue.ContainsKey(key))
                {
                    key++; // Handle score collisions
                }
                
                _priorityQueue.Add(key, queueItem);
            }

            var position = GetQueuePositionSync(request.Id);
            
            _logger.LogInformation("Added maintenance request {RequestId} to priority queue at position {Position} with priority {Priority}", 
                request.Id, position, request.Priority);

            return position;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding request {RequestId} to priority queue", request.Id);
            throw;
        }
    }

    public async Task<MaintenanceRequest?> GetNextRequestAsync()
    {
        try
        {
            PriorityQueueItem? nextItem = null;
            
            lock (_queueLock)
            {
                if (_priorityQueue.Any())
                {
                    var firstItem = _priorityQueue.First();
                    nextItem = firstItem.Value;
                    _priorityQueue.RemoveAt(0);
                }
            }

            if (nextItem == null)
                return null;

            var request = await _context.MaintenanceRequests
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == nextItem.RequestId);

            if (request == null)
            {
                _logger.LogWarning("Request {RequestId} in queue but not found in database", nextItem.RequestId);
                return await GetNextRequestAsync(); // Try next item
            }

            _logger.LogInformation("Retrieved next maintenance request {RequestId} from priority queue", request.Id);
            return request;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next request from priority queue");
            throw;
        }
    }

    public async Task<MaintenanceRequest?> GetNextRequestForTechnicianAsync(int technicianId, MaintenanceSpecialty? specialty = null)
    {
        try
        {
            var technician = await _context.Staff.FindAsync(technicianId);
            if (technician == null || technician.Role != StaffRole.Technician)
            {
                return null;
            }

            PriorityQueueItem? selectedItem = null;
            
            lock (_queueLock)
            {
                // Find the highest priority request that matches technician's specialty or is general
                selectedItem = _priorityQueue.Values
                    .Where(item => item.RequiredSpecialty == null || 
                                  item.RequiredSpecialty == specialty || 
                                  specialty == MaintenanceSpecialty.General)
                    .FirstOrDefault();

                if (selectedItem != null)
                {
                    var keyToRemove = _priorityQueue.FirstOrDefault(kvp => kvp.Value.RequestId == selectedItem.RequestId).Key;
                    if (keyToRemove != 0)
                    {
                        _priorityQueue.Remove(keyToRemove);
                    }
                }
            }

            if (selectedItem == null)
                return null;

            var request = await _context.MaintenanceRequests
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == selectedItem.RequestId);

            if (request != null)
            {
                _logger.LogInformation("Retrieved maintenance request {RequestId} for technician {TechnicianId} with specialty {Specialty}", 
                    request.Id, technicianId, specialty);
            }

            return request;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next request for technician {TechnicianId}", technicianId);
            throw;
        }
    }

    public async Task<bool> RemoveFromQueueAsync(int requestId)
    {
        try
        {
            bool removed = false;
            
            lock (_queueLock)
            {
                var itemToRemove = _priorityQueue.FirstOrDefault(kvp => kvp.Value.RequestId == requestId);
                if (itemToRemove.Key != 0)
                {
                    _priorityQueue.Remove(itemToRemove.Key);
                    removed = true;
                }
            }

            if (removed)
            {
                _logger.LogInformation("Removed maintenance request {RequestId} from priority queue", requestId);
            }

            return await Task.FromResult(removed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing request {RequestId} from priority queue", requestId);
            throw;
        }
    }

    public async Task<int> GetQueuePositionAsync(int requestId)
    {
        return await Task.FromResult(GetQueuePositionSync(requestId));
    }

    public async Task<TimeSpan> GetEstimatedWaitTimeAsync(int requestId)
    {
        try
        {
            var position = await GetQueuePositionAsync(requestId);
            if (position == -1)
                return TimeSpan.Zero;

            // Estimate based on average resolution time and available technicians
            var availableTechnicians = await _context.Staff
                .CountAsync(s => s.Role == StaffRole.Technician && s.Status == StaffStatus.Active);

            var averageResolutionMinutes = 45; // Default estimate
            var estimatedMinutes = (position - 1) * averageResolutionMinutes / Math.Max(availableTechnicians, 1);

            return TimeSpan.FromMinutes(estimatedMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating wait time for request {RequestId}", requestId);
            return TimeSpan.FromHours(2); // Default fallback
        }
    }

    public async Task<List<MaintenanceRequestDTO>> GetQueueStatusAsync(int count = 10)
    {
        try
        {
            var queueItems = new List<PriorityQueueItem>();
            
            lock (_queueLock)
            {
                queueItems = _priorityQueue.Values.Take(count).ToList();
            }

            var requestIds = queueItems.Select(qi => qi.RequestId).ToList();
            var requests = await _context.MaintenanceRequests
                .Include(r => r.Room)
                .Include(r => r.AssignedTechnician)
                .Where(r => requestIds.Contains(r.Id))
                .ToListAsync();

            var result = new List<MaintenanceRequestDTO>();
            
            for (int i = 0; i < queueItems.Count; i++)
            {
                var queueItem = queueItems[i];
                var request = requests.FirstOrDefault(r => r.Id == queueItem.RequestId);
                
                if (request != null)
                {
                    var dto = MapToMaintenanceRequestDTO(request);
                    dto.QueuePosition = i + 1;
                    dto.EstimatedWaitTime = await GetEstimatedWaitTimeAsync(request.Id);
                    result.Add(dto);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue status");
            throw;
        }
    }

    public async Task<PriorityQueueStatusDTO> GetPriorityQueueSummaryAsync()
    {
        try
        {
            var queueItems = new List<PriorityQueueItem>();
            
            lock (_queueLock)
            {
                queueItems = _priorityQueue.Values.ToList();
            }

            var availableTechnicians = await _context.Staff
                .CountAsync(s => s.Role == StaffRole.Technician && s.Status == StaffStatus.Active);

            var longestWait = queueItems.Any() 
                ? DateTime.UtcNow - queueItems.Min(qi => qi.QueuedAt)
                : TimeSpan.Zero;

            var averageWait = queueItems.Any()
                ? TimeSpan.FromMilliseconds(queueItems.Average(qi => (DateTime.UtcNow - qi.QueuedAt).TotalMilliseconds))
                : TimeSpan.Zero;

            var nextRequests = await GetQueueStatusAsync(5);

            return new PriorityQueueStatusDTO
            {
                TotalInQueue = queueItems.Count,
                CriticalInQueue = queueItems.Count(qi => qi.Priority == MaintenancePriority.Critical),
                HighInQueue = queueItems.Count(qi => qi.Priority == MaintenancePriority.High),
                NormalInQueue = queueItems.Count(qi => qi.Priority == MaintenancePriority.Normal),
                LowInQueue = queueItems.Count(qi => qi.Priority == MaintenancePriority.Low),
                AverageWaitTime = averageWait,
                LongestWaitTime = longestWait,
                AvailableTechnicians = availableTechnicians,
                NextRequests = nextRequests
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting priority queue summary");
            throw;
        }
    }

    public async Task RebalanceQueueAsync()
    {
        try
        {
            _logger.LogInformation("Starting priority queue rebalancing");

            var currentItems = new List<PriorityQueueItem>();
            
            lock (_queueLock)
            {
                currentItems = _priorityQueue.Values.ToList();
                _priorityQueue.Clear();
            }

            // Reload requests from database and re-add to queue
            foreach (var item in currentItems)
            {
                var request = await _context.MaintenanceRequests.FindAsync(item.RequestId);
                if (request != null && request.Status == MaintenanceStatus.Reported)
                {
                    await AddToQueueAsync(request);
                }
            }

            _logger.LogInformation("Completed priority queue rebalancing with {Count} items", currentItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebalancing priority queue");
            throw;
        }
    }

    // Helper methods
    private int GetQueuePositionSync(int requestId)
    {
        lock (_queueLock)
        {
            var position = 1;
            foreach (var item in _priorityQueue.Values)
            {
                if (item.RequestId == requestId)
                    return position;
                position++;
            }
            return -1; // Not found
        }
    }

    private static MaintenanceSpecialty? DetermineRequiredSpecialty(string description)
    {
        var lowerDesc = description.ToLower();
        
        if (lowerDesc.Contains("plumb") || lowerDesc.Contains("leak") || lowerDesc.Contains("water") || lowerDesc.Contains("toilet") || lowerDesc.Contains("shower"))
            return MaintenanceSpecialty.Plumbing;
        
        if (lowerDesc.Contains("electric") || lowerDesc.Contains("light") || lowerDesc.Contains("power") || lowerDesc.Contains("outlet"))
            return MaintenanceSpecialty.Electrical;
        
        if (lowerDesc.Contains("hvac") || lowerDesc.Contains("air") || lowerDesc.Contains("heat") || lowerDesc.Contains("temperature") || lowerDesc.Contains("ventilation"))
            return MaintenanceSpecialty.HVAC;
        
        if (lowerDesc.Contains("door") || lowerDesc.Contains("window") || lowerDesc.Contains("wood") || lowerDesc.Contains("cabinet"))
            return MaintenanceSpecialty.Carpentry;
        
        if (lowerDesc.Contains("tv") || lowerDesc.Contains("wifi") || lowerDesc.Contains("internet") || lowerDesc.Contains("computer"))
            return MaintenanceSpecialty.IT;
        
        if (lowerDesc.Contains("fridge") || lowerDesc.Contains("microwave") || lowerDesc.Contains("appliance"))
            return MaintenanceSpecialty.Appliances;
        
        if (lowerDesc.Contains("lock") || lowerDesc.Contains("key") || lowerDesc.Contains("security") || lowerDesc.Contains("safe"))
            return MaintenanceSpecialty.Security;
        
        return MaintenanceSpecialty.General;
    }

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

public class DescendingComparer : IComparer<int>
{
    public int Compare(int x, int y)
    {
        return y.CompareTo(x); // Reverse order for descending sort
    }
}