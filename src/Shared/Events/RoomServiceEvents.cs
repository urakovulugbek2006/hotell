using HotelOS.Shared.Models;

namespace HotelOS.Shared.Events;

public class OrderReceivedEvent : BaseEvent
{
    public override string EventType => "OrderReceived";
    
    public int OrderId { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public DateTime OrderTime { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public string? SpecialInstructions { get; set; }
}

public class OrderPreparingEvent : BaseEvent
{
    public override string EventType => "OrderPreparing";
    
    public int OrderId { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int ChefId { get; set; }
    public string ChefName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan EstimatedPreparationTime { get; set; }
}

public class OrderOutForDeliveryEvent : BaseEvent
{
    public override string EventType => "OrderOutForDelivery";
    
    public int OrderId { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int DeliveryStaffId { get; set; }
    public string DeliveryStaffName { get; set; } = string.Empty;
    public DateTime DeliveryStartTime { get; set; }
    public TimeSpan EstimatedDeliveryTime { get; set; }
}

public class OrderDeliveredEvent : BaseEvent
{
    public override string EventType => "OrderDelivered";
    
    public int OrderId { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime DeliveredTime { get; set; }
    public TimeSpan TotalOrderTime { get; set; }
    public decimal TotalAmount { get; set; }
    public bool CustomerSatisfied { get; set; } = true;
    public string? DeliveryNotes { get; set; }
}

public class OrderCancelledEvent : BaseEvent
{
    public override string EventType => "OrderCancelled";
    
    public int OrderId { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime CancelledTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool RefundIssued { get; set; }
    public decimal RefundAmount { get; set; }
}