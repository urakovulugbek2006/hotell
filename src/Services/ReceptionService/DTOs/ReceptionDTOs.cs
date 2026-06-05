using HotelOS.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace ReceptionService.DTOs;

public class CheckInRequest
{
    [Required]
    public int BookingId { get; set; }
    
    [Required]
    public int GuestId { get; set; }
    
    public int? FloorPreference { get; set; }
    
    public bool PreferElevator { get; set; }
    
    public bool PreferStairs { get; set; }
    
    public string? SpecialRequests { get; set; }
}

public class CheckInResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Room? AssignedRoom { get; set; }
    public Booking? Booking { get; set; }
    public Guest? Guest { get; set; }
    public DateTime CheckInTime { get; set; }
}

public class CheckOutRequest
{
    [Required]
    public int BookingId { get; set; }
    
    public PaymentMethod PaymentMethod { get; set; }
    
    public string? PaymentReference { get; set; }
    
    public decimal? PaymentAmount { get; set; }
    
    public string? Notes { get; set; }
}

public class CheckOutResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Bill? Bill { get; set; }
    public decimal TotalBill { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public DateTime CheckOutTime { get; set; }
    public Room? VacatedRoom { get; set; }
}

public class CreateBookingRequest
{
    [Required]
    public int GuestId { get; set; }
    
    [Required]
    public RoomType RequestedRoomType { get; set; }
    
    [Required]
    public DateTime CheckInDate { get; set; }
    
    [Required]
    public DateTime CheckOutDate { get; set; }
    
    public int? FloorPreference { get; set; }
    
    public bool NeedElevatorAccess { get; set; }
    
    public bool NeedStairsAccess { get; set; }
    
    public string? SpecialRequests { get; set; }
}

public class CreateBookingResponse
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Booking? Booking { get; set; }
    public decimal EstimatedTotal { get; set; }
}

public class RoomAvailabilityDTO
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public RoomType Type { get; set; }
    public RoomStatus Status { get; set; }
    public decimal NightlyRate { get; set; }
    public bool IsAccessible { get; set; }
    public bool NearElevator { get; set; }
    public bool NearStairs { get; set; }
    public DateTime? LastCleaned { get; set; }
}

public class ActiveBookingDTO
{
    public int Id { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public RoomType RequestedRoomType { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public DateTime? ActualCheckIn { get; set; }
    public decimal TotalAmount { get; set; }
    public int NumberOfNights { get; set; }
}