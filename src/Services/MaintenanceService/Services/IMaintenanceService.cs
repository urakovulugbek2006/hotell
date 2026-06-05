using HotelOS.Shared.Models;
using MaintenanceService.DTOs;

namespace MaintenanceService.Services;

public interface IMaintenanceService
{
    // Request Management
    Task<MaintenanceResponse> CreateRequestAsync(CreateMaintenanceRequestDTO request);
    Task<MaintenanceResponse> AssignRequestAsync(AssignMaintenanceRequestDTO request);
    Task<MaintenanceResponse> UpdateStatusAsync(UpdateMaintenanceStatusDTO request);
    Task<MaintenanceResponse> CompleteRequestAsync(CompleteMaintenanceRequestDTO request);
    Task<MaintenanceResponse> CancelRequestAsync(int requestId, string reason);
    
    // Workflow Management
    Task<MaintenanceResponse> StartWorkAsync(int requestId, int technicianId);
    Task<MaintenanceResponse> PauseWorkAsync(int requestId, int technicianId, string reason);
    Task<MaintenanceResponse> ResumeWorkAsync(int requestId, int technicianId);
    
    // Request Queries
    Task<IEnumerable<MaintenanceRequestDTO>> GetRequestsByStatusAsync(MaintenanceStatus status);
    Task<IEnumerable<MaintenanceRequestDTO>> GetRequestsByPriorityAsync(MaintenancePriority priority);
    Task<IEnumerable<MaintenanceRequestDTO>> GetRequestsByTechnicianAsync(int technicianId);
    Task<IEnumerable<MaintenanceRequestDTO>> GetRequestsByRoomAsync(int roomId);
    Task<IEnumerable<MaintenanceRequestDTO>> GetOverdueRequestsAsync();
    Task<MaintenanceRequestDTO?> GetRequestAsync(int requestId);
    
    // Technician Management
    Task<IEnumerable<TechnicianWorkloadDTO>> GetTechnicianWorkloadsAsync();
    Task<TechnicianWorkloadDTO?> GetTechnicianWorkloadAsync(int technicianId);
    Task<IEnumerable<Staff>> GetAvailableTechniciansAsync();
    Task<Staff?> GetBestTechnicianForRequestAsync(int requestId);
    
    // Reporting and Analytics
    Task<MaintenanceSummaryDTO> GetMaintenanceSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<PriorityQueueStatusDTO> GetQueueStatusAsync();
    
    // Auto-assignment
    Task AutoAssignRequestsAsync();
    Task<MaintenanceResponse> AutoAssignRequestAsync(int requestId);
}