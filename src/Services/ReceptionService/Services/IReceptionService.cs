using HotelOS.Shared.Models;
using ReceptionService.DTOs;

namespace ReceptionService.Services;

public interface IReceptionService
{
    Task<CheckInResponse> CheckInGuestAsync(CheckInRequest request);
    Task<CheckOutResponse> CheckOutGuestAsync(CheckOutRequest request);
    Task<CreateBookingResponse> CreateBookingAsync(CreateBookingRequest request);
    Task<IEnumerable<RoomAvailabilityDTO>> GetAvailableRoomsAsync(RoomType? roomType = null, int? floor = null);
    Task<IEnumerable<ActiveBookingDTO>> GetActiveBookingsAsync();
    Task<Guest?> GetGuestAsync(int guestId);
}