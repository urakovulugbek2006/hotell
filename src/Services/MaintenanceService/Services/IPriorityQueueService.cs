using HotelOS.Shared.Models;
using MaintenanceService.DTOs;

namespace MaintenanceService.Services;

public interface IPriorityQueueService
{
    Task<int> AddToQueueAsync(MaintenanceRequest request);
    Task<MaintenanceRequest?> GetNextRequestAsync();
    Task<MaintenanceRequest?> GetNextRequestForTechnicianAsync(int technicianId, MaintenanceSpecialty? specialty = null);
    Task<bool> RemoveFromQueueAsync(int requestId);
    Task<int> GetQueuePositionAsync(int requestId);
    Task<TimeSpan> GetEstimatedWaitTimeAsync(int requestId);
    Task<List<MaintenanceRequestDTO>> GetQueueStatusAsync(int count = 10);
    Task<PriorityQueueStatusDTO> GetPriorityQueueSummaryAsync();
    Task RebalanceQueueAsync();
}

public class PriorityQueueItem
{
    public int RequestId { get; set; }
    public MaintenancePriority Priority { get; set; }
    public DateTime QueuedAt { get; set; }
    public MaintenanceSpecialty? RequiredSpecialty { get; set; }
    public bool IsEmergency { get; set; }
    
    // Priority calculation score (higher = more priority)
    public int PriorityScore => CalculatePriorityScore();
    
    private int CalculatePriorityScore()
    {
        var baseScore = (int)Priority * 1000; // Critical=4000, High=3000, Normal=2000, Low=1000
        
        // Add urgency based on how long it's been waiting
        var waitingMinutes = (DateTime.UtcNow - QueuedAt).TotalMinutes;
        var urgencyScore = (int)(waitingMinutes * Priority.GetUrgencyMultiplier());
        
        // Emergency requests get massive boost
        var emergencyBonus = IsEmergency ? 10000 : 0;
        
        return baseScore + urgencyScore + emergencyBonus;
    }
}

public static class PriorityExtensions
{
    public static double GetUrgencyMultiplier(this MaintenancePriority priority)
    {
        return priority switch
        {
            MaintenancePriority.Critical => 5.0,
            MaintenancePriority.High => 3.0,
            MaintenancePriority.Normal => 1.0,
            MaintenancePriority.Low => 0.5,
            _ => 1.0
        };
    }
    
    public static TimeSpan GetMaxWaitTime(this MaintenancePriority priority)
    {
        return priority switch
        {
            MaintenancePriority.Critical => TimeSpan.FromMinutes(15),
            MaintenancePriority.High => TimeSpan.FromHours(2),
            MaintenancePriority.Normal => TimeSpan.FromHours(8),
            MaintenancePriority.Low => TimeSpan.FromHours(24),
            _ => TimeSpan.FromHours(8)
        };
    }
}