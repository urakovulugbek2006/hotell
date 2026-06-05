using HotelOS.Shared.Events;
using HotelOS.Shared.Infrastructure;
using HotelOS.Shared.Models;
using Microsoft.EntityFrameworkCore;
using ReceptionService.DTOs;

namespace ReceptionService.Services;

public class ReceptionService : IReceptionService
{
    private readonly HotelDbContext _context;
    private readonly IMessageBroker _messageBroker;
    private readonly IRoomAssignmentAlgorithm _roomAssignmentAlgorithm;
    private readonly IBillingService _billingService;
    private readonly ILogger<ReceptionService> _logger;

    public ReceptionService(
        HotelDbContext context,
        IMessageBroker messageBroker,
        IRoomAssignmentAlgorithm roomAssignmentAlgorithm,
        IBillingService billingService,
        ILogger<ReceptionService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        _roomAssignmentAlgorithm = roomAssignmentAlgorithm ?? throw new ArgumentNullException(nameof(roomAssignmentAlgorithm));
        _billingService = billingService ?? throw new ArgumentNullException(nameof(billingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CheckInResponse> CheckInGuestAsync(CheckInRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Validate booking exists and is confirmed
            var booking = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId && b.GuestId == request.GuestId);

            if (booking == null)
            {
                return new CheckInResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Booking not found or guest mismatch" 
                };
            }

            if (booking.Status != BookingStatus.Confirmed)
            {
                return new CheckInResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"Booking status is {booking.Status}, expected Confirmed" 
                };
            }

            // Check if already checked in
            if (booking.ActualCheckIn.HasValue)
            {
                return new CheckInResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Guest is already checked in" 
                };
            }

            Room assignedRoom;

            // If room is already assigned, use it (if still available)
            if (booking.RoomId.HasValue)
            {
                var preAssignedRoom = booking.Room;
                if (preAssignedRoom != null && preAssignedRoom.Status == RoomStatus.Clean)
                {
                    assignedRoom = preAssignedRoom;
                    _logger.LogInformation("Using pre-assigned room {RoomNumber} for booking {BookingId}", 
                        assignedRoom.RoomNumber, booking.Id);
                }
                else
                {
                    // Pre-assigned room is not available, need to reassign
                    _logger.LogWarning("Pre-assigned room {RoomNumber} is not available (status: {Status}), reassigning", 
                        preAssignedRoom?.RoomNumber, preAssignedRoom?.Status);
                    
                    var newRoom = await _roomAssignmentAlgorithm.AssignBestRoomAsync(
                        booking.RequestedRoomType, 
                        request.FloorPreference ?? booking.FloorPreference,
                        request.PreferElevator || booking.NeedElevatorAccess,
                        request.PreferStairs || booking.NeedStairsAccess);

                    if (newRoom == null)
                    {
                        return new CheckInResponse 
                        { 
                            IsSuccess = false, 
                            ErrorMessage = "No suitable rooms available for check-in" 
                        };
                    }
                    
                    assignedRoom = newRoom;
                }
            }
            else
            {
                // No room pre-assigned, use algorithm to assign best room
                assignedRoom = await _roomAssignmentAlgorithm.AssignBestRoomAsync(
                    booking.RequestedRoomType, 
                    request.FloorPreference ?? booking.FloorPreference,
                    request.PreferElevator || booking.NeedElevatorAccess,
                    request.PreferStairs || booking.NeedStairsAccess);

                if (assignedRoom == null)
                {
                    return new CheckInResponse 
                    { 
                        IsSuccess = false, 
                        ErrorMessage = "No suitable rooms available for check-in" 
                    };
                }
            }

            // Update booking and room status
            booking.RoomId = assignedRoom.Id;
            booking.ActualCheckIn = DateTime.UtcNow;
            booking.Status = BookingStatus.CheckedIn;
            booking.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(request.SpecialRequests))
            {
                booking.SpecialRequests = request.SpecialRequests;
            }

            assignedRoom.Status = RoomStatus.Occupied;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Publish events
            await PublishCheckInEvents(booking, assignedRoom);

            _logger.LogInformation("Successfully checked in guest {GuestName} (ID: {GuestId}) to room {RoomNumber}", 
                booking.Guest.FullName, booking.GuestId, assignedRoom.RoomNumber);

            return new CheckInResponse
            {
                IsSuccess = true,
                AssignedRoom = assignedRoom,
                Booking = booking,
                Guest = booking.Guest,
                CheckInTime = booking.ActualCheckIn.Value
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during check-in process for booking {BookingId}", request.BookingId);
            throw;
        }
    }

    public async Task<CheckOutResponse> CheckOutGuestAsync(CheckOutRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Get booking with related data
            var booking = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .Include(b => b.Bill)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId);

            if (booking == null)
            {
                return new CheckOutResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Booking not found" 
                };
            }

            if (booking.Status != BookingStatus.CheckedIn)
            {
                return new CheckOutResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = $"Guest is not checked in (status: {booking.Status})" 
                };
            }

            if (booking.Room == null)
            {
                return new CheckOutResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "No room associated with booking" 
                };
            }

            // Calculate final bill
            var bill = await _billingService.CalculateFinalBillAsync(booking.Id);

            // Process payment if provided
            if (request.PaymentAmount.HasValue && request.PaymentAmount > 0)
            {
                bill.PaidAmount += request.PaymentAmount.Value;
                bill.PaymentMethod = request.PaymentMethod;
                bill.PaymentReference = request.PaymentReference;
                bill.PaidAt = DateTime.UtcNow;
            }

            // Update bill status
            if (bill.PaidAmount >= bill.TotalAmount)
            {
                bill.Status = BillStatus.Paid;
            }
            else if (bill.PaidAmount > 0)
            {
                bill.Status = BillStatus.Partial;
            }

            // Update booking and room
            booking.ActualCheckOut = DateTime.UtcNow;
            booking.Status = BookingStatus.CheckedOut;
            booking.UpdatedAt = DateTime.UtcNow;

            var vacatedRoom = booking.Room;
            vacatedRoom.Status = RoomStatus.Dirty; // Room needs cleaning after checkout

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Publish events
            await PublishCheckOutEvents(booking, bill, vacatedRoom);

            _logger.LogInformation("Successfully checked out guest {GuestName} (ID: {GuestId}) from room {RoomNumber}, bill total: {TotalBill}", 
                booking.Guest.FullName, booking.GuestId, vacatedRoom.RoomNumber, bill.TotalAmount);

            return new CheckOutResponse
            {
                IsSuccess = true,
                Bill = bill,
                TotalBill = bill.TotalAmount,
                AmountPaid = bill.PaidAmount,
                BalanceDue = bill.BalanceDue,
                CheckOutTime = booking.ActualCheckOut.Value,
                VacatedRoom = vacatedRoom
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during check-out process for booking {BookingId}", request.BookingId);
            throw;
        }
    }

    public async Task<CreateBookingResponse> CreateBookingAsync(CreateBookingRequest request)
    {
        try
        {
            // Validate guest exists
            var guest = await _context.Guests.FindAsync(request.GuestId);
            if (guest == null)
            {
                return new CreateBookingResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Guest not found" 
                };
            }

            // Validate dates
            if (request.CheckInDate >= request.CheckOutDate)
            {
                return new CreateBookingResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Check-out date must be after check-in date" 
                };
            }

            if (request.CheckInDate < DateTime.UtcNow.Date)
            {
                return new CreateBookingResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Check-in date cannot be in the past" 
                };
            }

            // Calculate estimated total (basic room rate calculation)
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

            _logger.LogInformation("Created new booking {BookingId} for guest {GuestId}", booking.Id, request.GuestId);

            return new CreateBookingResponse
            {
                IsSuccess = true,
                Booking = booking,
                EstimatedTotal = estimatedTotal
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking for guest {GuestId}", request.GuestId);
            throw;
        }
    }

    public async Task<IEnumerable<RoomAvailabilityDTO>> GetAvailableRoomsAsync(RoomType? roomType = null, int? floor = null)
    {
        var query = _context.Rooms
            .Where(r => r.Status == RoomStatus.Available || r.Status == RoomStatus.Clean);

        if (roomType.HasValue)
        {
            query = query.Where(r => r.Type == roomType.Value);
        }

        if (floor.HasValue)
        {
            query = query.Where(r => r.Floor == floor.Value);
        }

        var rooms = await query
            .OrderBy(r => r.Floor)
            .ThenBy(r => r.RoomNumber)
            .Select(r => new RoomAvailabilityDTO
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                Type = r.Type,
                Status = r.Status,
                NightlyRate = r.NightlyRate,
                IsAccessible = r.IsAccessible,
                NearElevator = r.NearElevator,
                NearStairs = r.NearStairs,
                LastCleaned = r.LastCleaned
            })
            .ToListAsync();

        return rooms;
    }

    public async Task<IEnumerable<ActiveBookingDTO>> GetActiveBookingsAsync()
    {
        var bookings = await _context.Bookings
            .Include(b => b.Guest)
            .Include(b => b.Room)
            .Where(b => b.Status == BookingStatus.Confirmed || 
                       b.Status == BookingStatus.CheckedIn ||
                       b.Status == BookingStatus.Pending)
            .OrderBy(b => b.CheckInDate)
            .Select(b => new ActiveBookingDTO
            {
                Id = b.Id,
                GuestName = b.Guest.FullName,
                RoomNumber = b.Room != null ? b.Room.RoomNumber : null,
                RequestedRoomType = b.RequestedRoomType,
                Status = b.Status,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                ActualCheckIn = b.ActualCheckIn,
                TotalAmount = b.TotalAmount,
                NumberOfNights = b.NumberOfNights
            })
            .ToListAsync();

        return bookings;
    }

    public async Task<Guest?> GetGuestAsync(int guestId)
    {
        return await _context.Guests.FindAsync(guestId);
    }

    private async Task PublishCheckInEvents(Booking booking, Room room)
    {
        var guestCheckedInEvent = new GuestCheckedInEvent
        {
            BookingId = booking.Id,
            GuestId = booking.GuestId,
            RoomId = room.Id,
            GuestName = booking.Guest.FullName,
            RoomNumber = room.RoomNumber,
            CheckInTime = booking.ActualCheckIn!.Value,
            ExpectedCheckOut = booking.CheckOutDate,
            RoomType = room.Type
        };

        var roomAssignedEvent = new RoomAssignedEvent
        {
            BookingId = booking.Id,
            RoomId = room.Id,
            RoomNumber = room.RoomNumber,
            GuestId = booking.GuestId,
            GuestName = booking.Guest.FullName,
            AssignmentTime = booking.ActualCheckIn!.Value,
            RoomType = room.Type
        };

        await _messageBroker.PublishAsync(guestCheckedInEvent, EventTopics.GuestCheckedIn);
        await _messageBroker.PublishAsync(roomAssignedEvent, EventTopics.RoomAssigned);
    }

    private async Task PublishCheckOutEvents(Booking booking, Bill bill, Room room)
    {
        var guestCheckedOutEvent = new GuestCheckedOutEvent
        {
            BookingId = booking.Id,
            GuestId = booking.GuestId,
            RoomId = room.Id,
            GuestName = booking.Guest.FullName,
            RoomNumber = room.RoomNumber,
            CheckOutTime = booking.ActualCheckOut!.Value,
            TotalBill = bill.TotalAmount,
            BillPaid = bill.IsPaid
        };

        var roomVacatedEvent = new RoomVacatedEvent
        {
            RoomId = room.Id,
            RoomNumber = room.RoomNumber,
            VacatedTime = booking.ActualCheckOut!.Value,
            PreviousGuestId = booking.GuestId,
            PreviousGuestName = booking.Guest.FullName,
            NeedsCleaning = true
        };

        await _messageBroker.PublishAsync(guestCheckedOutEvent, EventTopics.GuestCheckedOut);
        await _messageBroker.PublishAsync(roomVacatedEvent, EventTopics.RoomVacated);
    }
}