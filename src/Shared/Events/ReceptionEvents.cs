using HotelOS.Shared.Models;

namespace HotelOS.Shared.Events;

public class GuestCheckedInEvent : BaseEvent
{
    public override string EventType => "GuestCheckedIn";
    
    public int BookingId { get; set; }
    public int GuestId { get; set; }
    public int RoomId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; }
    public DateTime ExpectedCheckOut { get; set; }
    public RoomType RoomType { get; set; }
}

public class GuestCheckedOutEvent : BaseEvent
{
    public override string EventType => "GuestCheckedOut";
    
    public int BookingId { get; set; }
    public int GuestId { get; set; }
    public int RoomId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime CheckOutTime { get; set; }
    public decimal TotalBill { get; set; }
    public bool BillPaid { get; set; }
}

public class RoomAssignedEvent : BaseEvent
{
    public override string EventType => "RoomAssigned";
    
    public int BookingId { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int GuestId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public DateTime AssignmentTime { get; set; }
    public RoomType RoomType { get; set; }
}

public class RoomVacatedEvent : BaseEvent
{
    public override string EventType => "RoomVacated";
    
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime VacatedTime { get; set; }
    public int PreviousGuestId { get; set; }
    public string PreviousGuestName { get; set; } = string.Empty;
    public bool NeedsCleaning { get; set; } = true;
}