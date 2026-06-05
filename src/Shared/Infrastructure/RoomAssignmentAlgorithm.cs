using HotelOS.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelOS.Shared.Infrastructure;

public interface IRoomAssignmentAlgorithm
{
    Task<Room?> AssignBestRoomAsync(RoomType requestedType, int? floorPreference = null, 
        bool preferElevator = false, bool preferStairs = false);
}

/// <summary>
/// Core room assignment algorithm implementing multi-criteria room selection:
/// 1. Room type match (exact match required)
/// 2. Cleanliness status (only Clean rooms eligible)
/// 3. Longest clean priority (room rotation)
/// 4. Floor preference (secondary filter)
/// 5. Proximity preference (tiebreaker)
/// </summary>
public class RoomAssignmentAlgorithm : IRoomAssignmentAlgorithm
{
    private readonly HotelDbContext _context;
    private readonly ILogger<RoomAssignmentAlgorithm> _logger;

    public RoomAssignmentAlgorithm(HotelDbContext context, ILogger<RoomAssignmentAlgorithm> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Room?> AssignBestRoomAsync(RoomType requestedType, int? floorPreference = null, 
        bool preferElevator = false, bool preferStairs = false)
    {
        try
        {
            _logger.LogInformation("Starting room assignment algorithm for type {RoomType}, floor {Floor}, elevator {Elevator}, stairs {Stairs}", 
                requestedType, floorPreference, preferElevator, preferStairs);

            // Step 1: Apply mandatory filters - room type and clean status
            var eligibleRooms = await _context.Rooms
                .Where(r => r.Type == requestedType && r.Status == RoomStatus.Clean)
                .ToListAsync();

            if (!eligibleRooms.Any())
            {
                _logger.LogWarning("No clean rooms available for type {RoomType}", requestedType);
                
                // Fallback: check for available rooms (not necessarily clean)
                var availableRooms = await _context.Rooms
                    .Where(r => r.Type == requestedType && r.Status == RoomStatus.Available)
                    .ToListAsync();
                
                if (!availableRooms.Any())
                {
                    _logger.LogWarning("No rooms available at all for type {RoomType}", requestedType);
                    return null;
                }
                
                eligibleRooms = availableRooms;
                _logger.LogInformation("Using {Count} available rooms as fallback for type {RoomType}", 
                    availableRooms.Count, requestedType);
            }

            _logger.LogInformation("Found {Count} eligible rooms for type {RoomType}", 
                eligibleRooms.Count, requestedType);

            // Step 2: Apply floor preference if specified
            var roomsToConsider = eligibleRooms;
            if (floorPreference.HasValue)
            {
                var preferredFloorRooms = eligibleRooms.Where(r => r.Floor == floorPreference.Value).ToList();
                if (preferredFloorRooms.Any())
                {
                    roomsToConsider = preferredFloorRooms;
                    _logger.LogInformation("Filtered to {Count} rooms on preferred floor {Floor}", 
                        preferredFloorRooms.Count, floorPreference.Value);
                }
                else
                {
                    _logger.LogInformation("No rooms available on preferred floor {Floor}, using all eligible rooms", 
                        floorPreference.Value);
                }
            }

            // Step 3: Sort by longest clean duration (room rotation)
            var sortedRooms = roomsToConsider
                .OrderBy(r => r.LastCleaned ?? DateTime.MinValue) // Rooms never cleaned go first
                .ThenBy(r => r.Id) // Consistent tiebreaker
                .ToList();

            _logger.LogInformation("Sorted rooms by cleaning duration, top candidate: Room {RoomNumber} (last cleaned: {LastCleaned})", 
                sortedRooms.First().RoomNumber, sortedRooms.First().LastCleaned);

            // Step 4: Apply proximity preference as final tiebreaker
            var bestRoom = ApplyProximityPreference(sortedRooms, preferElevator, preferStairs);

            _logger.LogInformation("Selected room {RoomNumber} for assignment", bestRoom.RoomNumber);
            return bestRoom;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in room assignment algorithm for type {RoomType}", requestedType);
            throw;
        }
    }

    private Room ApplyProximityPreference(List<Room> rooms, bool preferElevator, bool preferStairs)
    {
        if (!preferElevator && !preferStairs)
        {
            return rooms.First(); // No preference, return first (longest clean)
        }

        // Get rooms with the same (earliest) LastCleaned time for tiebreaking
        var earliestCleanTime = rooms.First().LastCleaned;
        var tieRooms = rooms.Where(r => r.LastCleaned == earliestCleanTime).ToList();

        if (tieRooms.Count == 1)
        {
            return tieRooms.First();
        }

        _logger.LogInformation("Applying proximity preference to {Count} rooms with same clean time", tieRooms.Count);

        // Apply proximity preferences
        Room? preferredRoom = null;

        if (preferElevator)
        {
            preferredRoom = tieRooms.FirstOrDefault(r => r.NearElevator);
            if (preferredRoom != null)
            {
                _logger.LogInformation("Selected room {RoomNumber} based on elevator preference", preferredRoom.RoomNumber);
                return preferredRoom;
            }
        }

        if (preferStairs)
        {
            preferredRoom = tieRooms.FirstOrDefault(r => r.NearStairs);
            if (preferredRoom != null)
            {
                _logger.LogInformation("Selected room {RoomNumber} based on stairs preference", preferredRoom.RoomNumber);
                return preferredRoom;
            }
        }

        // No proximity match found, return first from tie rooms
        var selectedRoom = tieRooms.First();
        _logger.LogInformation("No proximity preference match, selected room {RoomNumber} as first available", 
            selectedRoom.RoomNumber);
        
        return selectedRoom;
    }
}