using System.ComponentModel.DataAnnotations;

namespace HotelOS.Shared.Models;

public class Bill
{
    public int Id { get; set; }
    
    public int BookingId { get; set; }
    
    public decimal RoomCharges { get; set; }
    
    public decimal RoomServiceCharges { get; set; }
    
    public decimal AdditionalCharges { get; set; }
    
    public decimal Taxes { get; set; }
    
    public decimal Discounts { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    public decimal PaidAmount { get; set; }
    
    public BillStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? PaidAt { get; set; }
    
    public PaymentMethod? PaymentMethod { get; set; }
    
    public string? PaymentReference { get; set; }
    
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual Booking Booking { get; set; } = null!;
    public virtual ICollection<BillLineItem> LineItems { get; set; } = new List<BillLineItem>();
    
    // Computed properties
    public decimal BalanceDue => TotalAmount - PaidAmount;
    public bool IsPaid => Status == BillStatus.Paid;
    public decimal SubTotal => RoomCharges + RoomServiceCharges + AdditionalCharges;
}

public class BillLineItem
{
    public int Id { get; set; }
    
    public int BillId { get; set; }
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    
    public decimal UnitPrice { get; set; }
    
    public decimal TotalPrice => Quantity * UnitPrice;
    
    public DateTime ChargeDate { get; set; }
    
    public ChargeType ChargeType { get; set; }
    
    // Navigation properties
    public virtual Bill Bill { get; set; } = null!;
}

public enum BillStatus
{
    Pending = 1,
    Partial = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5
}

public enum PaymentMethod
{
    Cash = 1,
    CreditCard = 2,
    DebitCard = 3,
    BankTransfer = 4,
    DigitalWallet = 5,
    Cheque = 6
}

public enum ChargeType
{
    Room = 1,
    RoomService = 2,
    Minibar = 3,
    LateCheckout = 4,
    EarlyCheckin = 5,
    Parking = 6,
    Internet = 7,
    Other = 8
}