using FrontendService.DTOs;
using HotelOS.Shared.Infrastructure;
using HotelOS.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace FrontendService.Services;

public class FrontendService : IFrontendService
{
    private readonly HotelDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<FrontendService> _logger;
    private readonly IConfiguration _configuration;

    public FrontendService(
        HotelDbContext context,
        HttpClient httpClient,
        ILogger<FrontendService> logger,
        IConfiguration configuration)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    // Guest Management Methods
    public async Task<ApiResponse<GuestDTO>> CreateGuestAsync(CreateGuestDTO request)
    {
        try
        {
            var existingGuest = await _context.Guests
                .FirstOrDefaultAsync(g => g.Email.ToLower() == request.Email.ToLower());

            if (existingGuest != null)
            {
                return ApiResponse<GuestDTO>.Error("A guest with this email already exists");
            }

            var guest = new Guest
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                DateOfBirth = request.DateOfBirth,
                PassportNumber = request.PassportNumber,
                Nationality = request.Nationality,
                SpecialRequests = request.SpecialRequests,
                CreatedAt = DateTime.UtcNow
            };

            _context.Guests.Add(guest);
            await _context.SaveChangesAsync();

            var guestDTO = MapToGuestDTO(guest);
            _logger.LogInformation("Created new guest {GuestId} with email {Email}", guest.Id, guest.Email);

            return ApiResponse<GuestDTO>.Success(guestDTO);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating guest with email {Email}", request.Email);
            return ApiResponse<GuestDTO>.Error("An error occurred while creating the guest");
        }
    }

    public async Task<ApiResponse<GuestDTO>> UpdateGuestAsync(UpdateGuestDTO request)
    {
        try
        {
            var guest = await _context.Guests.FindAsync(request.Id);
            if (guest == null)
            {
                return ApiResponse<GuestDTO>.Error("Guest not found");
            }

            if (!string.IsNullOrEmpty(request.FirstName))
                guest.FirstName = request.FirstName;
            if (!string.IsNullOrEmpty(request.LastName))
                guest.LastName = request.LastName;
            if (!string.IsNullOrEmpty(request.Email))
                guest.Email = request.Email;
            if (!string.IsNullOrEmpty(request.PhoneNumber))
                guest.PhoneNumber = request.PhoneNumber;
            if (!string.IsNullOrEmpty(request.Address))
                guest.Address = request.Address;
            if (!string.IsNullOrEmpty(request.PassportNumber))
                guest.PassportNumber = request.PassportNumber;
            if (!string.IsNullOrEmpty(request.Nationality))
                guest.Nationality = request.Nationality;
            if (!string.IsNullOrEmpty(request.SpecialRequests))
                guest.SpecialRequests = request.SpecialRequests;

            await _context.SaveChangesAsync();

            var guestDTO = MapToGuestDTO(guest);
            _logger.LogInformation("Updated guest {GuestId}", guest.Id);

            return ApiResponse<GuestDTO>.Success(guestDTO);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating guest {GuestId}", request.Id);
            return ApiResponse<GuestDTO>.Error("An error occurred while updating the guest");
        }
    }

    public async Task<ApiResponse<GuestDTO>> GetGuestAsync(int guestId)
    {
        try
        {
            var guest = await _context.Guests.FindAsync(guestId);
            if (guest == null)
            {
                return ApiResponse<GuestDTO>.Error("Guest not found");
            }

            var guestDTO = MapToGuestDTO(guest);
            return ApiResponse<GuestDTO>.Success(guestDTO);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving guest {GuestId}", guestId);
            return ApiResponse<GuestDTO>.Error("An error occurred while retrieving the guest");
        }
    }

    public async Task<ApiResponse<GuestDTO>> GetGuestByEmailAsync(string email)
    {
        try
        {
            var guest = await _context.Guests
                .FirstOrDefaultAsync(g => g.Email.ToLower() == email.ToLower());
                
            if (guest == null)
            {
                return ApiResponse<GuestDTO>.Error("Guest not found");
            }

            var guestDTO = MapToGuestDTO(guest);
            return ApiResponse<GuestDTO>.Success(guestDTO);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving guest by email {Email}", email);
            return ApiResponse<GuestDTO>.Error("An error occurred while retrieving the guest");
        }
    }

    public async Task<ApiResponse<PaginatedResponse<GuestDTO>>> GetGuestsAsync(int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            var totalCount = await _context.Guests.CountAsync();
            var guests = await _context.Guests
                .OrderByDescending(g => g.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var guestDTOs = guests.Select(MapToGuestDTO).ToList();
            
            var response = new PaginatedResponse<GuestDTO>
            {
                Items = guestDTOs,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return ApiResponse<PaginatedResponse<GuestDTO>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving guests page {PageNumber}", pageNumber);
            return ApiResponse<PaginatedResponse<GuestDTO>>.Error("An error occurred while retrieving guests");
        }
    }

    // Booking Management Methods
    public async Task<ApiResponse<BookingDTO>> CreateBookingAsync(CreateBookingDTO request)
    {
        try
        {
            // Validate guest exists
            var guest = await _context.Guests.FindAsync(request.GuestId);
            if (guest == null)
            {
                return ApiResponse<BookingDTO>.Error("Guest not found");
            }

            // Validate dates
            if (request.CheckInDate >= request.CheckOutDate)
            {
                return ApiResponse<BookingDTO>.Error("Check-out date must be after check-in date");
            }

            if (request.CheckInDate < DateTime.UtcNow.Date)
            {
                return ApiResponse<BookingDTO>.Error("Check-in date cannot be in the past");
            }

            // Calculate estimated total
            var numberOfNights = (request.CheckOutDate - request.CheckInDate).Days;
            var sampleRoom = await _context.Rooms
                .Where(r => r.Type == request.RequestedRoomType)
                .FirstOrDefaultAsync();

            var estimatedTotal = sampleRoom?.NightlyRate * numberOfNights ?? 0;

            var booking = new Booking
            {
                GuestId = request.GuestId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                Status = BookingStatus.Pending,
                RequestedRoomType = request.RequestedRoomType,
                FloorPreference = request.FloorPreference,
                NeedElevatorAccess = request.NeedElevatorAccess,
                NeedStairsAccess = request.NeedStairsAccess,
                SpecialRequests = request.SpecialRequests,
                TotalAmount = estimatedTotal,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            var bookingDTO = await MapToBookingDTOAsync(booking);
            _logger.LogInformation("Created booking {BookingId} for guest {GuestId}", booking.Id, request.GuestId);

            return ApiResponse<BookingDTO>.Success(bookingDTO);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking for guest {GuestId}", request.GuestId);
            return ApiResponse<BookingDTO>.Error("An error occurred while creating the booking");
        }
    }

    public async Task<ApiResponse<BookingDTO>> GetBookingAsync(int bookingId)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return ApiResponse<BookingDTO>.Error("Booking not found");
            }

            var bookingDTO = await MapToBookingDTOAsync(booking);
            return ApiResponse<BookingDTO>.Success(bookingDTO);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking {BookingId}", bookingId);
            return ApiResponse<BookingDTO>.Error("An error occurred while retrieving the booking");
        }
    }

    public async Task<ApiResponse<PaginatedResponse<BookingDTO>>> GetBookingsByGuestAsync(int guestId, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            var totalCount = await _context.Bookings.CountAsync(b => b.GuestId == guestId);
            var bookings = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .Where(b => b.GuestId == guestId)
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var bookingDTOs = new List<BookingDTO>();
            foreach (var booking in bookings)
            {
                bookingDTOs.Add(await MapToBookingDTOAsync(booking));
            }

            var response = new PaginatedResponse<BookingDTO>
            {
                Items = bookingDTOs,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return ApiResponse<PaginatedResponse<BookingDTO>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bookings for guest {GuestId}", guestId);
            return ApiResponse<PaginatedResponse<BookingDTO>>.Error("An error occurred while retrieving bookings");
        }
    }

    public async Task<ApiResponse<PaginatedResponse<BookingDTO>>> GetActiveBookingsAsync(int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            var activeStatuses = new[] { BookingStatus.Confirmed, BookingStatus.CheckedIn, BookingStatus.Pending };
            
            var totalCount = await _context.Bookings.CountAsync(b => activeStatuses.Contains(b.Status));
            var bookings = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .Where(b => activeStatuses.Contains(b.Status))
                .OrderByDescending(b => b.CheckInDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var bookingDTOs = new List<BookingDTO>();
            foreach (var booking in bookings)
            {
                bookingDTOs.Add(await MapToBookingDTOAsync(booking));
            }

            var response = new PaginatedResponse<BookingDTO>
            {
                Items = bookingDTOs,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return ApiResponse<PaginatedResponse<BookingDTO>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active bookings");
            return ApiResponse<PaginatedResponse<BookingDTO>>.Error("An error occurred while retrieving active bookings");
        }
    }

    public async Task<ApiResponse<BookingDTO>> CancelBookingAsync(int bookingId, string reason)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return ApiResponse<BookingDTO>.Error("Booking not found");
            }

            if (booking.Status == BookingStatus.CheckedIn)
            {
                return ApiResponse<BookingDTO>.Error("Cannot cancel a booking that is already checked in");
            }

            if (booking.Status == BookingStatus.CheckedOut)
            {
                return ApiResponse<BookingDTO>.Error("Cannot cancel a completed booking");
            }

            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            var bookingDTO = await MapToBookingDTOAsync(booking);
            _logger.LogInformation("Cancelled booking {BookingId}, reason: {Reason}", bookingId, reason);

            return ApiResponse<BookingDTO>.Success(bookingDTO);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking {BookingId}", bookingId);
            return ApiResponse<BookingDTO>.Error("An error occurred while cancelling the booking");
        }
    }

    public async Task<ApiResponse<BookingDTO>> ModifyBookingAsync(int bookingId, CreateBookingDTO modifications)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return ApiResponse<BookingDTO>.Error("Booking not found");
            }

            if (booking.Status == BookingStatus.CheckedIn || booking.Status == BookingStatus.CheckedOut)
            {
                return ApiResponse<BookingDTO>.Error("Cannot modify a booking that is checked in or completed");
            }

            // Update booking details
            booking.CheckInDate = modifications.CheckInDate;
            booking.CheckOutDate = modifications.CheckOutDate;
            booking.RequestedRoomType = modifications.RequestedRoomType;
            booking.FloorPreference = modifications.FloorPreference;
            booking.NeedElevatorAccess = modifications.NeedElevatorAccess;
            booking.NeedStairsAccess = modifications.NeedStairsAccess;
            booking.SpecialRequests = modifications.SpecialRequests;
            booking.UpdatedAt = DateTime.UtcNow;

            // Recalculate total if dates changed
            var numberOfNights = (modifications.CheckOutDate - modifications.CheckInDate).Days;
            var sampleRoom = await _context.Rooms
                .Where(r => r.Type == modifications.RequestedRoomType)
                .FirstOrDefaultAsync();

            if (sampleRoom != null)
            {
                booking.TotalAmount = sampleRoom.NightlyRate * numberOfNights;
            }

            await _context.SaveChangesAsync();

            var bookingDTO = await MapToBookingDTOAsync(booking);
            _logger.LogInformation("Modified booking {BookingId}", bookingId);

            return ApiResponse<BookingDTO>.Success(bookingDTO);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error modifying booking {BookingId}", bookingId);
            return ApiResponse<BookingDTO>.Error("An error occurred while modifying the booking");
        }
    }

    // Room Availability Methods
    public async Task<ApiResponse<PaginatedResponse<RoomDTO>>> GetAvailableRoomsAsync(RoomAvailabilityQueryDTO query, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            var roomQuery = _context.Rooms.AsQueryable();

            // Apply filters
            if (query.RoomType.HasValue)
                roomQuery = roomQuery.Where(r => r.Type == query.RoomType.Value);

            if (query.Floor.HasValue)
                roomQuery = roomQuery.Where(r => r.Floor == query.Floor.Value);

            if (query.AccessibleOnly == true)
                roomQuery = roomQuery.Where(r => r.IsAccessible);

            if (query.NearElevator == true)
                roomQuery = roomQuery.Where(r => r.NearElevator);

            if (query.NearStairs == true)
                roomQuery = roomQuery.Where(r => r.NearStairs);

            if (query.MaxRate.HasValue)
                roomQuery = roomQuery.Where(r => r.NightlyRate <= query.MaxRate.Value);

            // Check availability for the requested dates
            var occupiedRoomIds = await _context.Bookings
                .Where(b => b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Confirmed)
                .Where(b => (b.CheckInDate < query.CheckOutDate && b.CheckOutDate > query.CheckInDate))
                .Select(b => b.RoomId)
                .ToListAsync();

            roomQuery = roomQuery.Where(r => !occupiedRoomIds.Contains(r.Id));

            var totalCount = await roomQuery.CountAsync();
            var rooms = await roomQuery
                .OrderBy(r => r.NightlyRate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var roomDTOs = rooms.Select(MapToRoomDTO).ToList();

            var response = new PaginatedResponse<RoomDTO>
            {
                Items = roomDTOs,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return ApiResponse<PaginatedResponse<RoomDTO>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available rooms");
            return ApiResponse<PaginatedResponse<RoomDTO>>.Error("An error occurred while retrieving available rooms");
        }
    }

    public async Task<ApiResponse<RoomDTO>> GetRoomAsync(int roomId)
    {
        try
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
            {
                return ApiResponse<RoomDTO>.Error("Room not found");
            }

            var roomDTO = MapToRoomDTO(room);
            return ApiResponse<RoomDTO>.Success(roomDTO);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving room {RoomId}", roomId);
            return ApiResponse<RoomDTO>.Error("An error occurred while retrieving the room");
        }
    }

    public async Task<ApiResponse<decimal>> GetRoomRateAsync(int roomId, DateTime checkIn, DateTime checkOut)
    {
        try
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
            {
                return ApiResponse<decimal>.Error("Room not found");
            }

            var numberOfNights = (checkOut - checkIn).Days;
            var totalRate = room.NightlyRate * numberOfNights;

            return ApiResponse<decimal>.Success(totalRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating room rate for room {RoomId}", roomId);
            return ApiResponse<decimal>.Error("An error occurred while calculating the room rate");
        }
    }

    // Room Service Methods (calling external Room Service API)
    public async Task<ApiResponse<OrderDTO>> CreateRoomServiceOrderAsync(CreateOrderDTO request)
    {
        try
        {
            var roomServiceUrl = _configuration["ServiceUrls:RoomService"] ?? "http://localhost:5003";
            var response = await _httpClient.PostAsJsonAsync($"{roomServiceUrl}/api/roomservice/orders", request);
            
            if (response.IsSuccessStatusCode)
            {
                var orderResponse = await response.Content.ReadFromJsonAsync<OrderResponse>();
                if (orderResponse?.IsSuccess == true && orderResponse.Order != null)
                {
                    var orderDTO = MapExternalOrderToDTO(orderResponse.Order);
                    return ApiResponse<OrderDTO>.Success(orderDTO);
                }
            }

            return ApiResponse<OrderDTO>.Error("Failed to create room service order");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room service order");
            return ApiResponse<OrderDTO>.Error("An error occurred while creating the room service order");
        }
    }

    public async Task<ApiResponse<OrderDTO>> GetOrderAsync(int orderId)
    {
        try
        {
            var roomServiceUrl = _configuration["ServiceUrls:RoomService"] ?? "http://localhost:5003";
            var response = await _httpClient.GetAsync($"{roomServiceUrl}/api/roomservice/orders/{orderId}");
            
            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<ExternalOrderDTO>();
                if (order != null)
                {
                    var orderDTO = MapExternalOrderToDTO(order);
                    return ApiResponse<OrderDTO>.Success(orderDTO);
                }
            }

            return ApiResponse<OrderDTO>.Error("Order not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", orderId);
            return ApiResponse<OrderDTO>.Error("An error occurred while retrieving the order");
        }
    }

    public async Task<ApiResponse<PaginatedResponse<OrderDTO>>> GetOrdersByRoomAsync(int roomId, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            var roomServiceUrl = _configuration["ServiceUrls:RoomService"] ?? "http://localhost:5003";
            var response = await _httpClient.GetAsync($"{roomServiceUrl}/api/roomservice/orders?roomId={roomId}");
            
            if (response.IsSuccessStatusCode)
            {
                var orders = await response.Content.ReadFromJsonAsync<List<ExternalOrderDTO>>();
                if (orders != null)
                {
                    var orderDTOs = orders.Select(MapExternalOrderToDTO).ToList();
                    
                    var paginatedOrders = orderDTOs
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    var paginatedResponse = new PaginatedResponse<OrderDTO>
                    {
                        Items = paginatedOrders,
                        TotalCount = orderDTOs.Count,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalPages = (int)Math.Ceiling(orderDTOs.Count / (double)pageSize),
                        HasPreviousPage = pageNumber > 1,
                        HasNextPage = pageNumber < (int)Math.Ceiling(orderDTOs.Count / (double)pageSize)
                    };

                    return ApiResponse<PaginatedResponse<OrderDTO>>.Success(paginatedResponse);
                }
            }

            return ApiResponse<PaginatedResponse<OrderDTO>>.Error("Failed to retrieve orders");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for room {RoomId}", roomId);
            return ApiResponse<PaginatedResponse<OrderDTO>>.Error("An error occurred while retrieving orders");
        }
    }

    public async Task<ApiResponse<OrderDTO>> CancelOrderAsync(int orderId, string reason)
    {
        try
        {
            var roomServiceUrl = _configuration["ServiceUrls:RoomService"] ?? "http://localhost:5003";
            var cancelRequest = new { Reason = reason };
            var response = await _httpClient.PostAsJsonAsync($"{roomServiceUrl}/api/roomservice/orders/{orderId}/cancel", cancelRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var orderResponse = await response.Content.ReadFromJsonAsync<OrderResponse>();
                if (orderResponse?.IsSuccess == true && orderResponse.Order != null)
                {
                    var orderDTO = MapExternalOrderToDTO(orderResponse.Order);
                    return ApiResponse<OrderDTO>.Success(orderDTO);
                }
            }

            return ApiResponse<OrderDTO>.Error("Failed to cancel order");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            return ApiResponse<OrderDTO>.Error("An error occurred while cancelling the order");
        }
    }

    public async Task<ApiResponse<PaginatedResponse<MenuItemDTO>>> GetMenuAsync(MenuCategory? category = null)
    {
        try
        {
            var roomServiceUrl = _configuration["ServiceUrls:RoomService"] ?? "http://localhost:5003";
            var categoryParam = category.HasValue ? $"?category={category}" : "";
            var response = await _httpClient.GetAsync($"{roomServiceUrl}/api/roomservice/menu{categoryParam}");
            
            if (response.IsSuccessStatusCode)
            {
                var menuItems = await response.Content.ReadFromJsonAsync<List<MenuItemDTO>>();
                if (menuItems != null)
                {
                    var paginatedResponse = new PaginatedResponse<MenuItemDTO>
                    {
                        Items = menuItems,
                        TotalCount = menuItems.Count,
                        PageNumber = 1,
                        PageSize = menuItems.Count,
                        TotalPages = 1,
                        HasPreviousPage = false,
                        HasNextPage = false
                    };

                    return ApiResponse<PaginatedResponse<MenuItemDTO>>.Success(paginatedResponse);
                }
            }

            return ApiResponse<PaginatedResponse<MenuItemDTO>>.Error("Failed to retrieve menu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving menu");
            return ApiResponse<PaginatedResponse<MenuItemDTO>>.Error("An error occurred while retrieving the menu");
        }
    }

    // Maintenance Request Methods (calling external Maintenance Service API)
    public async Task<ApiResponse<MaintenanceRequestDTO>> CreateMaintenanceRequestAsync(CreateMaintenanceRequestDTO request)
    {
        try
        {
            var maintenanceUrl = _configuration["ServiceUrls:MaintenanceService"] ?? "http://localhost:5004";
            var response = await _httpClient.PostAsJsonAsync($"{maintenanceUrl}/api/maintenance/requests", request);
            
            if (response.IsSuccessStatusCode)
            {
                var maintenanceResponse = await response.Content.ReadFromJsonAsync<MaintenanceResponse>();
                if (maintenanceResponse?.IsSuccess == true && maintenanceResponse.Request != null)
                {
                    var requestDTO = MapExternalMaintenanceRequestToDTO(maintenanceResponse.Request);
                    return ApiResponse<MaintenanceRequestDTO>.Success(requestDTO);
                }
            }

            return ApiResponse<MaintenanceRequestDTO>.Error("Failed to create maintenance request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating maintenance request");
            return ApiResponse<MaintenanceRequestDTO>.Error("An error occurred while creating the maintenance request");
        }
    }

    public async Task<ApiResponse<MaintenanceRequestDTO>> GetMaintenanceRequestAsync(int requestId)
    {
        try
        {
            var maintenanceUrl = _configuration["ServiceUrls:MaintenanceService"] ?? "http://localhost:5004";
            var response = await _httpClient.GetAsync($"{maintenanceUrl}/api/maintenance/requests/{requestId}");
            
            if (response.IsSuccessStatusCode)
            {
                var request = await response.Content.ReadFromJsonAsync<ExternalMaintenanceRequestDTO>();
                if (request != null)
                {
                    var requestDTO = MapExternalMaintenanceRequestToDTO(request);
                    return ApiResponse<MaintenanceRequestDTO>.Success(requestDTO);
                }
            }

            return ApiResponse<MaintenanceRequestDTO>.Error("Maintenance request not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving maintenance request {RequestId}", requestId);
            return ApiResponse<MaintenanceRequestDTO>.Error("An error occurred while retrieving the maintenance request");
        }
    }

    public async Task<ApiResponse<PaginatedResponse<MaintenanceRequestDTO>>> GetMaintenanceRequestsByRoomAsync(int roomId, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            var maintenanceUrl = _configuration["ServiceUrls:MaintenanceService"] ?? "http://localhost:5004";
            var response = await _httpClient.GetAsync($"{maintenanceUrl}/api/maintenance/requests?roomId={roomId}");
            
            if (response.IsSuccessStatusCode)
            {
                var requests = await response.Content.ReadFromJsonAsync<List<ExternalMaintenanceRequestDTO>>();
                if (requests != null)
                {
                    var requestDTOs = requests.Select(MapExternalMaintenanceRequestToDTO).ToList();
                    
                    var paginatedRequests = requestDTOs
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    var paginatedResponse = new PaginatedResponse<MaintenanceRequestDTO>
                    {
                        Items = paginatedRequests,
                        TotalCount = requestDTOs.Count,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalPages = (int)Math.Ceiling(requestDTOs.Count / (double)pageSize),
                        HasPreviousPage = pageNumber > 1,
                        HasNextPage = pageNumber < (int)Math.Ceiling(requestDTOs.Count / (double)pageSize)
                    };

                    return ApiResponse<PaginatedResponse<MaintenanceRequestDTO>>.Success(paginatedResponse);
                }
            }

            return ApiResponse<PaginatedResponse<MaintenanceRequestDTO>>.Error("Failed to retrieve maintenance requests");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving maintenance requests for room {RoomId}", roomId);
            return ApiResponse<PaginatedResponse<MaintenanceRequestDTO>>.Error("An error occurred while retrieving maintenance requests");
        }
    }

    // Billing Methods (calling external Reception Service API)
    public async Task<ApiResponse<BillDTO>> GetBillAsync(int bookingId)
    {
        try
        {
            var receptionUrl = _configuration["ServiceUrls:ReceptionService"] ?? "http://localhost:5001";
            // Note: This assumes the reception service has a bill endpoint
            // In actual implementation, you might need to adapt based on the actual API structure
            
            var booking = await _context.Bookings
                .Include(b => b.Bill)
                .ThenInclude(bill => bill!.LineItems)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking?.Bill == null)
            {
                return ApiResponse<BillDTO>.Error("Bill not found");
            }

            var billDTO = MapToBillDTO(booking.Bill);
            return ApiResponse<BillDTO>.Success(billDTO);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bill for booking {BookingId}", bookingId);
            return ApiResponse<BillDTO>.Error("An error occurred while retrieving the bill");
        }
    }

    public async Task<ApiResponse<PaginatedResponse<BillDTO>>> GetBillsByGuestAsync(int guestId, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            var bookingIds = await _context.Bookings
                .Where(b => b.GuestId == guestId)
                .Select(b => b.Id)
                .ToListAsync();

            var bills = await _context.Bills
                .Include(b => b.LineItems)
                .Where(b => bookingIds.Contains(b.BookingId))
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalCount = await _context.Bills
                .CountAsync(b => bookingIds.Contains(b.BookingId));

            var billDTOs = bills.Select(MapToBillDTO).ToList();

            var response = new PaginatedResponse<BillDTO>
            {
                Items = billDTOs,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return ApiResponse<PaginatedResponse<BillDTO>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bills for guest {GuestId}", guestId);
            return ApiResponse<PaginatedResponse<BillDTO>>.Error("An error occurred while retrieving bills");
        }
    }

    // Authentication Methods
    public async Task<LoginResponse> AuthenticateGuestAsync(LoginRequest request)
    {
        try
        {
            var guest = await _context.Guests
                .FirstOrDefaultAsync(g => g.Email.ToLower() == request.Email.ToLower());

            if (guest == null)
            {
                return new LoginResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid email or password"
                };
            }

            // Verify the password against the stored hash.
            if (string.IsNullOrEmpty(guest.PasswordHash) || guest.PasswordHash != HashPassword(request.Password))
            {
                return new LoginResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid email or password"
                };
            }
            
            var token = GenerateJwtToken(guest);
            var guestDTO = MapToGuestDTO(guest);

            return new LoginResponse
            {
                IsSuccess = true,
                Token = token,
                Guest = guestDTO,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating guest with email {Email}", request.Email);
            return new LoginResponse
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred during authentication"
            };
        }
    }

    public async Task<ApiResponse<GuestDTO>> RegisterGuestAsync(CreateGuestDTO request)
    {
        try
        {
            // Check if guest already exists
            var existingGuest = await _context.Guests
                .FirstOrDefaultAsync(g => g.Email.ToLower() == request.Email.ToLower());

            if (existingGuest != null)
            {
                return ApiResponse<GuestDTO>.Error("A guest with this email already exists");
            }

            return await CreateGuestAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering guest with email {Email}", request.Email);
            return ApiResponse<GuestDTO>.Error("An error occurred during registration");
        }
    }

    // Helper Methods
    private static GuestDTO MapToGuestDTO(Guest guest)
    {
        return new GuestDTO
        {
            Id = guest.Id,
            FirstName = guest.FirstName,
            LastName = guest.LastName,
            FullName = guest.FullName,
            Email = guest.Email,
            PhoneNumber = guest.PhoneNumber,
            Address = guest.Address,
            DateOfBirth = guest.DateOfBirth,
            PassportNumber = guest.PassportNumber,
            Nationality = guest.Nationality,
            IsVip = guest.IsVip,
            SpecialRequests = guest.SpecialRequests,
            CreatedAt = guest.CreatedAt
        };
    }

    private async Task<BookingDTO> MapToBookingDTOAsync(Booking booking)
    {
        return new BookingDTO
        {
            Id = booking.Id,
            GuestId = booking.GuestId,
            Guest = MapToGuestDTO(booking.Guest),
            RoomId = booking.RoomId,
            Room = booking.Room != null ? MapToRoomDTO(booking.Room) : null,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            ActualCheckIn = booking.ActualCheckIn,
            ActualCheckOut = booking.ActualCheckOut,
            Status = booking.Status,
            RequestedRoomType = booking.RequestedRoomType,
            FloorPreference = booking.FloorPreference,
            NeedElevatorAccess = booking.NeedElevatorAccess,
            NeedStairsAccess = booking.NeedStairsAccess,
            SpecialRequests = booking.SpecialRequests,
            TotalAmount = booking.TotalAmount,
            PaidAmount = booking.PaidAmount,
            BalanceDue = booking.BalanceDue,
            NumberOfNights = booking.NumberOfNights,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt
        };
    }

    private static RoomDTO MapToRoomDTO(Room room)
    {
        return new RoomDTO
        {
            Id = room.Id,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            Type = room.Type,
            Status = room.Status,
            NightlyRate = room.NightlyRate,
            IsAccessible = room.IsAccessible,
            NearElevator = room.NearElevator,
            NearStairs = room.NearStairs,
            LastCleaned = room.LastCleaned,
            RoomTypeDescription = GetRoomTypeDescription(room.Type),
            Amenities = GetRoomAmenities(room.Type, room.IsAccessible),
            IsAvailable = room.Status == RoomStatus.Available || room.Status == RoomStatus.Clean
        };
    }

    private static BillDTO MapToBillDTO(Bill bill)
    {
        return new BillDTO
        {
            Id = bill.Id,
            BookingId = bill.BookingId,
            RoomCharges = bill.RoomCharges,
            RoomServiceCharges = bill.RoomServiceCharges,
            AdditionalCharges = bill.AdditionalCharges,
            Taxes = bill.Taxes,
            Discounts = bill.Discounts,
            SubTotal = bill.SubTotal,
            TotalAmount = bill.TotalAmount,
            PaidAmount = bill.PaidAmount,
            BalanceDue = bill.BalanceDue,
            Status = bill.Status,
            CreatedAt = bill.CreatedAt,
            PaidAt = bill.PaidAt,
            PaymentMethod = bill.PaymentMethod,
            LineItems = bill.LineItems.Select(li => new BillLineItemDTO
            {
                Id = li.Id,
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                TotalPrice = li.TotalPrice,
                ChargeDate = li.ChargeDate,
                ChargeType = li.ChargeType
            }).ToList()
        };
    }

    private static OrderDTO MapExternalOrderToDTO(ExternalOrderDTO externalOrder)
    {
        return new OrderDTO
        {
            Id = externalOrder.Id,
            RoomId = externalOrder.RoomId,
            RoomNumber = externalOrder.RoomNumber,
            GuestName = externalOrder.GuestName,
            Status = externalOrder.Status,
            OrderTime = externalOrder.OrderTime,
            PreparedTime = externalOrder.PreparedTime,
            DeliveredTime = externalOrder.DeliveredTime,
            TotalAmount = externalOrder.TotalAmount,
            SpecialInstructions = externalOrder.SpecialInstructions,
            Items = externalOrder.Items.Select(i => new OrderItemDTO
            {
                ItemName = i.ItemName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Notes = i.Notes
            }).ToList(),
            StatusDescription = GetOrderStatusDescription(externalOrder.Status),
            EstimatedDeliveryTime = GetEstimatedDeliveryTime(externalOrder.Status, externalOrder.OrderTime)
        };
    }

    private static MaintenanceRequestDTO MapExternalMaintenanceRequestToDTO(ExternalMaintenanceRequestDTO externalRequest)
    {
        return new MaintenanceRequestDTO
        {
            Id = externalRequest.Id,
            RoomId = externalRequest.RoomId,
            RoomNumber = externalRequest.RoomNumber,
            Description = externalRequest.Description,
            Priority = externalRequest.Priority,
            Status = externalRequest.Status,
            ReportedAt = externalRequest.ReportedAt,
            AssignedAt = externalRequest.AssignedAt,
            CompletedAt = externalRequest.CompletedAt,
            AssignedTechnicianName = externalRequest.AssignedTechnicianName,
            ReportedBy = externalRequest.ReportedBy,
            ResolutionNotes = externalRequest.ResolutionNotes,
            StatusDescription = GetMaintenanceStatusDescription(externalRequest.Status),
            PriorityDescription = GetMaintenancePriorityDescription(externalRequest.Priority),
            EstimatedResolutionTime = GetEstimatedResolutionTime(externalRequest.Priority)
        };
    }

    private string GenerateJwtToken(Guest guest)
    {
        // Note: In a real implementation, use proper JWT token generation with signing key
        // This is a simplified version for demo purposes
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{guest.Id}:{guest.Email}:{DateTime.UtcNow.Ticks}"));
    }

    // Hashes a password with SHA-256. Plain-text passwords are never stored.
    private static string HashPassword(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static string GetRoomTypeDescription(RoomType type) => type switch
    {
        RoomType.Single => "Single Room - Perfect for solo travelers",
        RoomType.Double => "Double Room - Comfortable for couples or friends",
        RoomType.Suite => "Suite - Luxury accommodation with separate living area",
        RoomType.Accessible => "Accessible Room - Designed for guests with mobility needs",
        _ => "Standard Room"
    };

    private static List<string> GetRoomAmenities(RoomType type, bool isAccessible)
    {
        var amenities = new List<string> { "Free WiFi", "Air Conditioning", "Private Bathroom", "Daily Housekeeping" };
        
        if (type == RoomType.Suite)
        {
            amenities.AddRange(new[] { "Separate Living Area", "Kitchenette", "Balcony", "Premium Bedding" });
        }
        else if (type == RoomType.Double || type == RoomType.Suite)
        {
            amenities.Add("King Size Bed");
        }

        if (isAccessible)
        {
            amenities.AddRange(new[] { "Wheelchair Accessible", "Roll-in Shower", "Grab Bars" });
        }

        return amenities;
    }

    private static string GetOrderStatusDescription(OrderStatus status) => status switch
    {
        OrderStatus.Received => "Order received and being processed",
        OrderStatus.Preparing => "Your order is being prepared",
        OrderStatus.OutForDelivery => "Order is on its way to your room",
        OrderStatus.Delivered => "Order has been delivered",
        OrderStatus.Cancelled => "Order has been cancelled",
        _ => "Unknown status"
    };

    private static TimeSpan? GetEstimatedDeliveryTime(OrderStatus status, DateTime orderTime) => status switch
    {
        OrderStatus.Received => TimeSpan.FromMinutes(45),
        OrderStatus.Preparing => TimeSpan.FromMinutes(30),
        OrderStatus.OutForDelivery => TimeSpan.FromMinutes(10),
        _ => null
    };

    private static string GetMaintenanceStatusDescription(MaintenanceStatus status) => status switch
    {
        MaintenanceStatus.Reported => "Request has been submitted",
        MaintenanceStatus.Assigned => "Technician has been assigned",
        MaintenanceStatus.InProgress => "Work is in progress",
        MaintenanceStatus.Completed => "Issue has been resolved",
        MaintenanceStatus.Cancelled => "Request has been cancelled",
        _ => "Unknown status"
    };

    private static string GetMaintenancePriorityDescription(MaintenancePriority priority) => priority switch
    {
        MaintenancePriority.Low => "Low priority - will be addressed within 24 hours",
        MaintenancePriority.Normal => "Normal priority - will be addressed within 8 hours",
        MaintenancePriority.High => "High priority - will be addressed within 2 hours",
        MaintenancePriority.Critical => "Critical - immediate attention required",
        _ => "Standard priority"
    };

    private static TimeSpan GetEstimatedResolutionTime(MaintenancePriority priority) => priority switch
    {
        MaintenancePriority.Critical => TimeSpan.FromMinutes(15),
        MaintenancePriority.High => TimeSpan.FromHours(2),
        MaintenancePriority.Normal => TimeSpan.FromHours(8),
        MaintenancePriority.Low => TimeSpan.FromHours(24),
        _ => TimeSpan.FromHours(4)
    };
}

// External service DTOs
public class ExternalOrderDTO
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime OrderTime { get; set; }
    public DateTime? PreparedTime { get; set; }
    public DateTime? DeliveredTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<ExternalOrderItemDTO> Items { get; set; } = new();
}

public class ExternalOrderItemDTO
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}

public class ExternalMaintenanceRequestDTO
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? AssignedTechnicianName { get; set; }
    public string? ReportedBy { get; set; }
    public string? ResolutionNotes { get; set; }
}

public class OrderResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public ExternalOrderDTO? Order { get; set; }
}

public class MaintenanceResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public ExternalMaintenanceRequestDTO? Request { get; set; }
}