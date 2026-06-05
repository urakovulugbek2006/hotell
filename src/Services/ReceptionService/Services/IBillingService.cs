using HotelOS.Shared.Models;

namespace ReceptionService.Services;

public interface IBillingService
{
    Task<Bill> CalculateFinalBillAsync(int bookingId);
    Task<decimal> CalculateRoomChargesAsync(int bookingId);
    Task<decimal> CalculateRoomServiceChargesAsync(int bookingId);
    Task<decimal> CalculateAdditionalChargesAsync(int bookingId);
    Task<decimal> CalculateTaxesAsync(decimal subtotal);
    Task<Bill> CreateBillAsync(int bookingId);
}