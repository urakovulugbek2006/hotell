using HotelOS.Shared.Events;
using HotelOS.Shared.Infrastructure;
using HotelOS.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReceptionService.DTOs;
using ReceptionService.Services;

namespace ReceptionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReceptionController : ControllerBase
{
    private readonly IReceptionService _receptionService;
    private readonly ILogger<ReceptionController> _logger;

    public ReceptionController(IReceptionService receptionService, ILogger<ReceptionController> logger)
    {
        _receptionService = receptionService ?? throw new ArgumentNullException(nameof(receptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("checkin")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
    {
        try
        {
            _logger.LogInformation("Processing check-in request for guest {GuestId} with booking {BookingId}", 
                request.GuestId, request.BookingId);

            var result = await _receptionService.CheckInGuestAsync(request);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully checked in guest {GuestId} to room {RoomNumber}", 
                    request.GuestId, result.AssignedRoom?.RoomNumber);
                return Ok(result);
            }

            _logger.LogWarning("Check-in failed for guest {GuestId}: {Error}", 
                request.GuestId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing check-in for guest {GuestId}", request.GuestId);
            return StatusCode(500, new { error = "An error occurred during check-in" });
        }
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request)
    {
        try
        {
            _logger.LogInformation("Processing check-out request for booking {BookingId}", request.BookingId);

            var result = await _receptionService.CheckOutGuestAsync(request);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully checked out guest from booking {BookingId}, total bill: {TotalBill}", 
                    request.BookingId, result.TotalBill);
                return Ok(result);
            }

            _logger.LogWarning("Check-out failed for booking {BookingId}: {Error}", 
                request.BookingId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing check-out for booking {BookingId}", request.BookingId);
            return StatusCode(500, new { error = "An error occurred during check-out" });
        }
    }

    [HttpGet("rooms/available")]
    public async Task<IActionResult> GetAvailableRooms([FromQuery] RoomType? roomType = null, [FromQuery] int? floor = null)
    {
        try
        {
            _logger.LogInformation("Fetching available rooms - Type: {RoomType}, Floor: {Floor}", roomType, floor);

            var rooms = await _receptionService.GetAvailableRoomsAsync(roomType, floor);
            
            _logger.LogInformation("Found {Count} available rooms", rooms.Count());
            return Ok(rooms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available rooms");
            return StatusCode(500, new { error = "An error occurred while fetching available rooms" });
        }
    }

    [HttpGet("bookings/active")]
    public async Task<IActionResult> GetActiveBookings()
    {
        try
        {
            _logger.LogInformation("Fetching active bookings");

            var bookings = await _receptionService.GetActiveBookingsAsync();
            
            _logger.LogInformation("Found {Count} active bookings", bookings.Count());
            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching active bookings");
            return StatusCode(500, new { error = "An error occurred while fetching active bookings" });
        }
    }

    [HttpPost("bookings")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        try
        {
            _logger.LogInformation("Creating new booking for guest {GuestId}", request.GuestId);

            var result = await _receptionService.CreateBookingAsync(request);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully created booking {BookingId} for guest {GuestId}", 
                    result.Booking?.Id, request.GuestId);
                return Ok(result);
            }

            _logger.LogWarning("Booking creation failed for guest {GuestId}: {Error}", 
                request.GuestId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking for guest {GuestId}", request.GuestId);
            return StatusCode(500, new { error = "An error occurred while creating booking" });
        }
    }

    [HttpGet("guests/{guestId}")]
    public async Task<IActionResult> GetGuest(int guestId)
    {
        try
        {
            var guest = await _receptionService.GetGuestAsync(guestId);
            
            if (guest == null)
            {
                return NotFound(new { error = "Guest not found" });
            }

            return Ok(guest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching guest {GuestId}", guestId);
            return StatusCode(500, new { error = "An error occurred while fetching guest information" });
        }
    }
}