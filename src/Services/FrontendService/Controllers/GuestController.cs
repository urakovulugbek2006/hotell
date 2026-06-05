using FrontendService.DTOs;
using FrontendService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FrontendService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GuestController : ControllerBase
{
    private readonly IFrontendService _frontendService;
    private readonly ILogger<GuestController> _logger;

    public GuestController(IFrontendService frontendService, ILogger<GuestController> logger)
    {
        _frontendService = frontendService ?? throw new ArgumentNullException(nameof(frontendService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateGuestDTO request)
    {
        try
        {
            var result = await _frontendService.RegisterGuestAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering guest");
            return StatusCode(500, new { error = "An error occurred during registration" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _frontendService.AuthenticateGuestAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { error = "An error occurred during login" });
        }
    }

    [HttpGet("{guestId}")]
    public async Task<IActionResult> GetGuest(int guestId)
    {
        try
        {
            var result = await _frontendService.GetGuestAsync(guestId);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving guest {GuestId}", guestId);
            return StatusCode(500, new { error = "An error occurred while retrieving guest information" });
        }
    }

    [HttpPut("{guestId}")]
    public async Task<IActionResult> UpdateGuest(int guestId, [FromBody] UpdateGuestDTO request)
    {
        try
        {
            request.Id = guestId;
            var result = await _frontendService.UpdateGuestAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating guest {GuestId}", guestId);
            return StatusCode(500, new { error = "An error occurred while updating guest information" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetGuests([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _frontendService.GetGuestsAsync(pageNumber, pageSize);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving guests");
            return StatusCode(500, new { error = "An error occurred while retrieving guests" });
        }
    }

    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetGuestByEmail(string email)
    {
        try
        {
            var result = await _frontendService.GetGuestByEmailAsync(email);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }

            return NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving guest by email {Email}", email);
            return StatusCode(500, new { error = "An error occurred while retrieving guest information" });
        }
    }
}