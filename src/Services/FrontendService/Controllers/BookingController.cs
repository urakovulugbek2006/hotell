using FrontendService.DTOs;
using FrontendService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontendService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly IFrontendService _frontendService;
    private readonly ILogger<BookingController> _logger;

    public BookingController(IFrontendService frontendService, ILogger<BookingController> logger)
    {
        _frontendService = frontendService ?? throw new ArgumentNullException(nameof(frontendService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDTO request)
    {
        try
        {
            var result = await _frontendService.CreateBookingAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            return StatusCode(500, new { error = "An error occurred while creating the booking" });
        }
    }

    [HttpGet("{bookingId}")]
    public async Task<IActionResult> GetBooking(int bookingId)
    {
        try
        {
            var result = await _frontendService.GetBookingAsync(bookingId);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking {BookingId}", bookingId);
            return StatusCode(500, new { error = "An error occurred while retrieving the booking" });
        }
    }

    [HttpGet("guest/{guestId}")]
    public async Task<IActionResult> GetBookingsByGuest(int guestId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _frontendService.GetBookingsByGuestAsync(guestId, pageNumber, pageSize);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bookings for guest {GuestId}", guestId);
            return StatusCode(500, new { error = "An error occurred while retrieving bookings" });
        }
    }

    [HttpPost("{bookingId}/cancel")]
    public async Task<IActionResult> CancelBooking(int bookingId, [FromBody] CancelBookingRequest request)
    {
        try
        {
            var result = await _frontendService.CancelBookingAsync(bookingId, request.Reason);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking {BookingId}", bookingId);
            return StatusCode(500, new { error = "An error occurred while cancelling the booking" });
        }
    }

    [HttpPut("{bookingId}")]
    public async Task<IActionResult> ModifyBooking(int bookingId, [FromBody] CreateBookingDTO modifications)
    {
        try
        {
            var result = await _frontendService.ModifyBookingAsync(bookingId, modifications);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error modifying booking {BookingId}", bookingId);
            return StatusCode(500, new { error = "An error occurred while modifying the booking" });
        }
    }
}

public class CancelBookingRequest
{
    public string Reason { get; set; } = string.Empty;
}