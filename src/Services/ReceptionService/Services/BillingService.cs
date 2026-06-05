using HotelOS.Shared.Infrastructure;
using HotelOS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ReceptionService.Services;

public class BillingService : IBillingService
{
    private readonly HotelDbContext _context;
    private readonly ILogger<BillingService> _logger;
    private const decimal TaxRate = 0.10m; // 10% tax rate

    public BillingService(HotelDbContext context, ILogger<BillingService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Bill> CalculateFinalBillAsync(int bookingId)
    {
        _logger.LogInformation("Calculating final bill for booking {BookingId}", bookingId);

        var existingBill = await _context.Bills
            .Include(b => b.LineItems)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        if (existingBill != null)
        {
            _logger.LogInformation("Updating existing bill {BillId} for booking {BookingId}", existingBill.Id, bookingId);
            return await UpdateBillAsync(existingBill);
        }

        _logger.LogInformation("Creating new bill for booking {BookingId}", bookingId);
        return await CreateBillAsync(bookingId);
    }

    public async Task<Bill> CreateBillAsync(int bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
            throw new ArgumentException($"Booking {bookingId} not found");
        }

        var roomCharges = await CalculateRoomChargesAsync(bookingId);
        var roomServiceCharges = await CalculateRoomServiceChargesAsync(bookingId);
        var additionalCharges = await CalculateAdditionalChargesAsync(bookingId);
        var subtotal = roomCharges + roomServiceCharges + additionalCharges;
        var taxes = await CalculateTaxesAsync(subtotal);

        var bill = new Bill
        {
            BookingId = bookingId,
            RoomCharges = roomCharges,
            RoomServiceCharges = roomServiceCharges,
            AdditionalCharges = additionalCharges,
            Taxes = taxes,
            Discounts = 0, // No discounts in this implementation
            TotalAmount = subtotal + taxes,
            PaidAmount = 0,
            Status = BillStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bills.Add(bill);

        // Add line items
        await AddRoomChargeLineItems(bill, booking);
        await AddRoomServiceLineItems(bill, bookingId);
        await AddAdditionalChargeLineItems(bill, bookingId);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Created bill {BillId} for booking {BookingId} with total amount {TotalAmount}", 
            bill.Id, bookingId, bill.TotalAmount);

        return bill;
    }

    public async Task<decimal> CalculateRoomChargesAsync(int bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking?.Room == null)
        {
            return 0;
        }

        var checkInDate = booking.ActualCheckIn ?? booking.CheckInDate;
        var checkOutDate = booking.ActualCheckOut ?? booking.CheckOutDate;
        var numberOfNights = (checkOutDate - checkInDate).Days;

        // Ensure at least 1 night is charged
        numberOfNights = Math.Max(numberOfNights, 1);

        var roomCharges = booking.Room.NightlyRate * numberOfNights;

        _logger.LogDebug("Room charges for booking {BookingId}: {Nights} nights × {Rate} = {Total}", 
            bookingId, numberOfNights, booking.Room.NightlyRate, roomCharges);

        return roomCharges;
    }

    public async Task<decimal> CalculateRoomServiceChargesAsync(int bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking?.Room == null)
        {
            return 0;
        }

        var roomServiceCharges = await _context.RoomServiceOrders
            .Where(o => o.RoomId == booking.Room.Id && 
                       o.BookingId == bookingId &&
                       o.Status == OrderStatus.Delivered)
            .SumAsync(o => o.TotalAmount);

        _logger.LogDebug("Room service charges for booking {BookingId}: {Total}", bookingId, roomServiceCharges);

        return roomServiceCharges;
    }

    public async Task<decimal> CalculateAdditionalChargesAsync(int bookingId)
    {
        // In a real implementation, this would include minibar, parking, late checkout fees, etc.
        // For now, we'll return 0
        var additionalCharges = 0m;

        _logger.LogDebug("Additional charges for booking {BookingId}: {Total}", bookingId, additionalCharges);

        return additionalCharges;
    }

    public async Task<decimal> CalculateTaxesAsync(decimal subtotal)
    {
        await Task.CompletedTask; // Placeholder for async operations
        var taxes = subtotal * TaxRate;
        
        _logger.LogDebug("Calculated taxes: {Subtotal} × {TaxRate} = {Taxes}", subtotal, TaxRate, taxes);
        
        return Math.Round(taxes, 2);
    }

    private async Task<Bill> UpdateBillAsync(Bill existingBill)
    {
        var bookingId = existingBill.BookingId;

        // Recalculate all charges
        existingBill.RoomCharges = await CalculateRoomChargesAsync(bookingId);
        existingBill.RoomServiceCharges = await CalculateRoomServiceChargesAsync(bookingId);
        existingBill.AdditionalCharges = await CalculateAdditionalChargesAsync(bookingId);

        var subtotal = existingBill.RoomCharges + existingBill.RoomServiceCharges + existingBill.AdditionalCharges;
        existingBill.Taxes = await CalculateTaxesAsync(subtotal);
        existingBill.TotalAmount = subtotal + existingBill.Taxes - existingBill.Discounts;

        // Update line items
        _context.BillLineItems.RemoveRange(existingBill.LineItems);
        
        var booking = await _context.Bookings.Include(b => b.Room).FirstAsync(b => b.Id == bookingId);
        await AddRoomChargeLineItems(existingBill, booking);
        await AddRoomServiceLineItems(existingBill, bookingId);
        await AddAdditionalChargeLineItems(existingBill, bookingId);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated bill {BillId} for booking {BookingId} with new total amount {TotalAmount}", 
            existingBill.Id, bookingId, existingBill.TotalAmount);

        return existingBill;
    }

    private async Task AddRoomChargeLineItems(Bill bill, Booking booking)
    {
        if (booking.Room == null) return;

        var checkInDate = booking.ActualCheckIn ?? booking.CheckInDate;
        var checkOutDate = booking.ActualCheckOut ?? booking.CheckOutDate;
        var numberOfNights = Math.Max((checkOutDate - checkInDate).Days, 1);

        var roomLineItem = new BillLineItem
        {
            BillId = bill.Id,
            Description = $"Room {booking.Room.RoomNumber} - {numberOfNights} night(s)",
            Quantity = numberOfNights,
            UnitPrice = booking.Room.NightlyRate,
            ChargeDate = checkInDate,
            ChargeType = ChargeType.Room
        };

        _context.BillLineItems.Add(roomLineItem);
        await Task.CompletedTask;
    }

    private async Task AddRoomServiceLineItems(Bill bill, int bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking?.Room == null) return;

        var roomServiceOrders = await _context.RoomServiceOrders
            .Include(o => o.Items)
            .Where(o => o.RoomId == booking.Room.Id && 
                       o.BookingId == bookingId &&
                       o.Status == OrderStatus.Delivered)
            .ToListAsync();

        foreach (var order in roomServiceOrders)
        {
            var lineItem = new BillLineItem
            {
                BillId = bill.Id,
                Description = $"Room Service Order #{order.Id}",
                Quantity = 1,
                UnitPrice = order.TotalAmount,
                ChargeDate = order.DeliveredTime ?? order.OrderTime,
                ChargeType = ChargeType.RoomService
            };

            _context.BillLineItems.Add(lineItem);
        }
    }

    private async Task AddAdditionalChargeLineItems(Bill bill, int bookingId)
    {
        // Placeholder for additional charges (minibar, parking, etc.)
        // In a real implementation, you would query for these charges from relevant tables
        await Task.CompletedTask;
    }
}