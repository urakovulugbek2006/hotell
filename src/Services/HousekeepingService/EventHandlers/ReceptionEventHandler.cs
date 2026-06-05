using HotelOS.Shared.Events;
using HotelOS.Shared.Infrastructure;
using HousekeepingService.Services;

namespace HousekeepingService.EventHandlers;

public class ReceptionEventHandler : IEventHandler<RoomVacatedEvent>
{
    private readonly IHousekeepingService _housekeepingService;
    private readonly ILogger<ReceptionEventHandler> _logger;

    public ReceptionEventHandler(IHousekeepingService housekeepingService, ILogger<ReceptionEventHandler> logger)
    {
        _housekeepingService = housekeepingService ?? throw new ArgumentNullException(nameof(housekeepingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(RoomVacatedEvent eventData)
    {
        try
        {
            _logger.LogInformation("Handling room vacated event for room {RoomNumber} (ID: {RoomId})", 
                eventData.RoomNumber, eventData.RoomId);

            await _housekeepingService.HandleRoomVacatedAsync(eventData.RoomId, eventData.PreviousGuestName);

            _logger.LogInformation("Successfully processed room vacated event for room {RoomNumber}", 
                eventData.RoomNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling room vacated event for room {RoomId}: {EventId}", 
                eventData.RoomId, eventData.EventId);
            throw;
        }
    }
}