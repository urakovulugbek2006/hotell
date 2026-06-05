using HousekeepingService.DTOs;
using HousekeepingService.Models;
using HotelOS.Shared.Models;

namespace HousekeepingService.Services;

public interface IHousekeepingService
{
    // Cleaning Task Management
    Task<CleaningResponse> StartCleaningAsync(StartCleaningRequest request);
    Task<CleaningResponse> CompleteCleaningAsync(CompleteCleaningRequest request);
    Task<CleaningResponse> AssignCleaningTaskAsync(AssignCleaningTaskRequest request);
    Task<CleaningResponse> UpdateRoomStatusAsync(UpdateRoomStatusRequest request);
    
    // Task Queries
    Task<IEnumerable<CleaningTaskDTO>> GetPendingTasksAsync();
    Task<IEnumerable<CleaningTaskDTO>> GetTasksByHousekeeperAsync(int housekeeperId);
    Task<IEnumerable<CleaningTaskDTO>> GetOverdueTasksAsync();
    Task<CleaningTaskDTO?> GetActiveTaskForRoomAsync(int roomId);
    
    // Room Management
    Task<IEnumerable<Room>> GetRoomsByStatusAsync(RoomStatus status);
    Task<RoomStatusSummaryDTO> GetRoomStatusSummaryAsync();
    Task<Room?> GetRoomAsync(int roomId);
    
    // Housekeeper Management
    Task<IEnumerable<HousekeeperWorkloadDTO>> GetHousekeeperWorkloadsAsync();
    Task<HousekeeperWorkloadDTO?> GetHousekeeperWorkloadAsync(int housekeeperId);
    Task<IEnumerable<Staff>> GetAvailableHousekeepersAsync();
    
    // Event Handlers
    Task HandleRoomVacatedAsync(int roomId, string previousGuestName);
}