using HotelOS.Shared.Events;
using HotelOS.Shared.Infrastructure;
using HotelOS.Shared.Models;
using Microsoft.EntityFrameworkCore;
using RoomService.DTOs;

namespace RoomService.Services;

public class RoomService : IRoomService
{
    private readonly HotelDbContext _context;
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<RoomService> _logger;

    public RoomService(
        HotelDbContext context,
        IMessageBroker messageBroker,
        ILogger<RoomService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Validate room exists
            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room == null)
            {
                return new OrderResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Room not found" 
                };
            }

            // Validate booking if provided
            Booking? booking = null;
            if (request.BookingId.HasValue)
            {
                booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.Id == request.BookingId.Value && 
                                            b.RoomId == request.RoomId &&
                                            b.Status == BookingStatus.CheckedIn);
                
                if (booking == null)
                {
                    return new OrderResponse 
                    { 
                        IsSuccess = false, 
                        ErrorMessage = "Active booking not found for this room" 
                    };
                }
            }

            // Calculate total amount
            var totalAmount = request.Items.Sum(item => item.Quantity * item.UnitPrice);

            // Create order
            var order = new RoomServiceOrder
            {
                RoomId = request.RoomId,
                BookingId = request.BookingId,
                GuestName = request.GuestName,
                Status = OrderStatus.Received,
                OrderTime = DateTime.UtcNow,
                SpecialInstructions = request.SpecialInstructions,
                TotalAmount = totalAmount
            };

            _context.RoomServiceOrders.Add(order);
            await _context.SaveChangesAsync();

            // Create order items
            foreach (var itemRequest in request.Items)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ItemName = itemRequest.ItemName,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = itemRequest.UnitPrice,
                    Notes = itemRequest.Notes
                };

                _context.OrderItems.Add(orderItem);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Load the order with items for response
            var createdOrder = await GetOrderWithItemsAsync(order.Id);

            // Publish event
            var orderReceivedEvent = new OrderReceivedEvent
            {
                OrderId = order.Id,
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                GuestName = request.GuestName,
                OrderTime = order.OrderTime,
                TotalAmount = totalAmount,
                ItemCount = request.Items.Count,
                SpecialInstructions = request.SpecialInstructions
            };

            await _messageBroker.PublishAsync(orderReceivedEvent, EventTopics.OrderReceived);

            _logger.LogInformation("Created room service order {OrderId} for room {RoomNumber} with total {TotalAmount}", 
                order.Id, room.RoomNumber, totalAmount);

            return new OrderResponse
            {
                IsSuccess = true,
                Order = createdOrder
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating room service order for room {RoomId}", request.RoomId);
            throw;
        }
    }

    public async Task<OrderResponse> UpdateOrderStatusAsync(UpdateOrderStatusRequest request)
    {
        try
        {
            var order = await _context.RoomServiceOrders
                .Include(o => o.Room)
                .Include(o => o.AssignedStaff)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order == null)
            {
                return new OrderResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Order not found" 
                };
            }

            var previousStatus = order.Status;
            order.Status = request.NewStatus;

            Staff? staff = null;
            if (request.StaffId.HasValue)
            {
                staff = await _context.Staff.FindAsync(request.StaffId.Value);
                order.AssignedStaffId = request.StaffId.Value;
            }

            // Update timestamps based on status
            switch (request.NewStatus)
            {
                case OrderStatus.Preparing:
                    // No specific timestamp - handled by StartPreparationAsync
                    break;
                case OrderStatus.OutForDelivery:
                    order.PreparedTime = DateTime.UtcNow;
                    break;
                case OrderStatus.Delivered:
                    order.DeliveredTime = DateTime.UtcNow;
                    break;
            }

            await _context.SaveChangesAsync();

            // Publish appropriate event
            await PublishStatusChangeEvent(order, previousStatus, staff);

            _logger.LogInformation("Updated order {OrderId} status from {PreviousStatus} to {NewStatus}", 
                request.OrderId, previousStatus, request.NewStatus);

            var updatedOrder = await GetOrderWithItemsAsync(order.Id);
            return new OrderResponse
            {
                IsSuccess = true,
                Order = updatedOrder
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for order {OrderId}", request.OrderId);
            throw;
        }
    }

    public async Task<OrderResponse> AssignOrderAsync(AssignOrderRequest request)
    {
        try
        {
            var order = await _context.RoomServiceOrders.FindAsync(request.OrderId);
            var staff = await _context.Staff.FindAsync(request.StaffId);

            if (order == null || staff == null)
            {
                return new OrderResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Order or staff member not found" 
                };
            }

            if (staff.Role != StaffRole.Chef && staff.Role != StaffRole.RoomServiceStaff)
            {
                return new OrderResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Staff member is not authorized for kitchen work" 
                };
            }

            order.AssignedStaffId = request.StaffId;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Assigned order {OrderId} to staff {StaffName}", 
                request.OrderId, staff.FullName);

            var assignedOrder = await GetOrderWithItemsAsync(order.Id);
            return new OrderResponse
            {
                IsSuccess = true,
                Order = assignedOrder
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning order {OrderId}", request.OrderId);
            throw;
        }
    }

    public async Task<OrderResponse> CancelOrderAsync(int orderId, string reason)
    {
        try
        {
            var order = await _context.RoomServiceOrders
                .Include(o => o.Room)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return new OrderResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Order not found" 
                };
            }

            if (order.Status == OrderStatus.Delivered)
            {
                return new OrderResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Cannot cancel a delivered order" 
                };
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();

            // Publish cancellation event
            var cancelledEvent = new OrderCancelledEvent
            {
                OrderId = order.Id,
                RoomId = order.RoomId,
                RoomNumber = order.Room.RoomNumber,
                CancelledTime = DateTime.UtcNow,
                Reason = reason,
                RefundIssued = true,
                RefundAmount = order.TotalAmount
            };

            await _messageBroker.PublishAsync(cancelledEvent, EventTopics.OrderCancelled);

            _logger.LogInformation("Cancelled order {OrderId} for room {RoomNumber}, reason: {Reason}", 
                orderId, order.Room.RoomNumber, reason);

            var cancelledOrder = await GetOrderWithItemsAsync(order.Id);
            return new OrderResponse
            {
                IsSuccess = true,
                Order = cancelledOrder
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<OrderResponse> StartPreparationAsync(int orderId, int chefId)
    {
        try
        {
            var order = await _context.RoomServiceOrders
                .Include(o => o.Room)
                .FirstOrDefaultAsync(o => o.Id == orderId);
                
            var chef = await _context.Staff.FindAsync(chefId);

            if (order == null || chef == null)
            {
                return new OrderResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Order or chef not found" 
                };
            }

            order.Status = OrderStatus.Preparing;
            order.AssignedStaffId = chefId;
            await _context.SaveChangesAsync();

            // Publish event
            var preparingEvent = new OrderPreparingEvent
            {
                OrderId = order.Id,
                RoomId = order.RoomId,
                RoomNumber = order.Room.RoomNumber,
                ChefId = chef.Id,
                ChefName = chef.FullName,
                StartTime = DateTime.UtcNow,
                EstimatedPreparationTime = TimeSpan.FromMinutes(30) // Default estimate
            };

            await _messageBroker.PublishAsync(preparingEvent, EventTopics.OrderPreparing);

            _logger.LogInformation("Started preparation for order {OrderId} by chef {ChefName}", 
                orderId, chef.FullName);

            var updatedOrder = await GetOrderWithItemsAsync(order.Id);
            return new OrderResponse { IsSuccess = true, Order = updatedOrder };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting preparation for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<OrderResponse> CompletePreparationAsync(int orderId, int chefId)
    {
        try
        {
            var order = await _context.RoomServiceOrders.FindAsync(orderId);
            
            if (order == null || order.AssignedStaffId != chefId)
            {
                return new OrderResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Order not found or not assigned to this chef" 
                };
            }

            order.Status = OrderStatus.OutForDelivery;
            order.PreparedTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Completed preparation for order {OrderId}", orderId);

            var updatedOrder = await GetOrderWithItemsAsync(order.Id);
            return new OrderResponse { IsSuccess = true, Order = updatedOrder };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing preparation for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<OrderResponse> StartDeliveryAsync(int orderId, int deliveryStaffId)
    {
        try
        {
            var order = await _context.RoomServiceOrders
                .Include(o => o.Room)
                .FirstOrDefaultAsync(o => o.Id == orderId);
                
            var deliveryStaff = await _context.Staff.FindAsync(deliveryStaffId);

            if (order == null || deliveryStaff == null)
            {
                return new OrderResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Order or delivery staff not found" 
                };
            }

            if (order.Status != OrderStatus.OutForDelivery)
            {
                return new OrderResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Order is not ready for delivery" 
                };
            }

            // Publish delivery started event
            var outForDeliveryEvent = new OrderOutForDeliveryEvent
            {
                OrderId = order.Id,
                RoomId = order.RoomId,
                RoomNumber = order.Room.RoomNumber,
                DeliveryStaffId = deliveryStaff.Id,
                DeliveryStaffName = deliveryStaff.FullName,
                DeliveryStartTime = DateTime.UtcNow,
                EstimatedDeliveryTime = TimeSpan.FromMinutes(10)
            };

            await _messageBroker.PublishAsync(outForDeliveryEvent, EventTopics.OrderOutForDelivery);

            _logger.LogInformation("Started delivery for order {OrderId} by {DeliveryStaffName}", 
                orderId, deliveryStaff.FullName);

            var updatedOrder = await GetOrderWithItemsAsync(order.Id);
            return new OrderResponse { IsSuccess = true, Order = updatedOrder };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting delivery for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<OrderResponse> CompleteDeliveryAsync(int orderId, int deliveryStaffId)
    {
        try
        {
            var order = await _context.RoomServiceOrders
                .Include(o => o.Room)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return new OrderResponse 
                { 
                    IsSuccess = false, 
                    ErrorMessage = "Order not found" 
                };
            }

            order.Status = OrderStatus.Delivered;
            order.DeliveredTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Publish delivery completed event
            var deliveredEvent = new OrderDeliveredEvent
            {
                OrderId = order.Id,
                RoomId = order.RoomId,
                RoomNumber = order.Room.RoomNumber,
                DeliveredTime = order.DeliveredTime.Value,
                TotalOrderTime = order.DeliveredTime.Value - order.OrderTime,
                TotalAmount = order.TotalAmount,
                CustomerSatisfied = true,
                DeliveryNotes = "Order delivered successfully"
            };

            await _messageBroker.PublishAsync(deliveredEvent, EventTopics.OrderDelivered);

            _logger.LogInformation("Completed delivery for order {OrderId} to room {RoomNumber}", 
                orderId, order.Room.RoomNumber);

            var updatedOrder = await GetOrderWithItemsAsync(order.Id);
            return new OrderResponse { IsSuccess = true, Order = updatedOrder };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing delivery for order {OrderId}", orderId);
            throw;
        }
    }

    // Query methods
    public async Task<IEnumerable<OrderDTO>> GetOrdersByStatusAsync(OrderStatus status)
    {
        var orders = await _context.RoomServiceOrders
            .Include(o => o.Room)
            .Include(o => o.AssignedStaff)
            .Include(o => o.Items)
            .Where(o => o.Status == status)
            .OrderBy(o => o.OrderTime)
            .ToListAsync();

        return orders.Select(MapToOrderDTO);
    }

    public async Task<IEnumerable<OrderDTO>> GetOrdersByRoomAsync(int roomId)
    {
        var orders = await _context.RoomServiceOrders
            .Include(o => o.Room)
            .Include(o => o.AssignedStaff)
            .Include(o => o.Items)
            .Where(o => o.RoomId == roomId)
            .OrderByDescending(o => o.OrderTime)
            .ToListAsync();

        return orders.Select(MapToOrderDTO);
    }

    public async Task<IEnumerable<OrderDTO>> GetOrdersByStaffAsync(int staffId)
    {
        var orders = await _context.RoomServiceOrders
            .Include(o => o.Room)
            .Include(o => o.AssignedStaff)
            .Include(o => o.Items)
            .Where(o => o.AssignedStaffId == staffId)
            .OrderByDescending(o => o.OrderTime)
            .ToListAsync();

        return orders.Select(MapToOrderDTO);
    }

    public async Task<IEnumerable<OrderDTO>> GetOverdueOrdersAsync()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-1);
        
        var orders = await _context.RoomServiceOrders
            .Include(o => o.Room)
            .Include(o => o.AssignedStaff)
            .Include(o => o.Items)
            .Where(o => o.Status != OrderStatus.Delivered && 
                       o.Status != OrderStatus.Cancelled &&
                       o.OrderTime < cutoffTime)
            .OrderBy(o => o.OrderTime)
            .ToListAsync();

        return orders.Select(MapToOrderDTO);
    }

    public async Task<OrderDTO?> GetOrderAsync(int orderId)
    {
        return await GetOrderWithItemsAsync(orderId);
    }

    public async Task<IEnumerable<MenuItemDTO>> GetMenuItemsAsync(MenuCategory? category = null)
    {
        // For this implementation, return hardcoded menu items
        var menuItems = GetSampleMenuItems();
        
        if (category.HasValue)
        {
            menuItems = menuItems.Where(m => m.Category == category.Value);
        }

        return await Task.FromResult(menuItems);
    }

    public async Task<MenuItemDTO?> GetMenuItemAsync(int menuItemId)
    {
        var menuItems = await GetMenuItemsAsync();
        return menuItems.FirstOrDefault(m => m.Id == menuItemId);
    }

    public async Task<IEnumerable<KitchenWorkloadDTO>> GetKitchenWorkloadAsync()
    {
        var kitchenStaff = await _context.Staff
            .Where(s => s.Role == StaffRole.Chef || s.Role == StaffRole.RoomServiceStaff)
            .ToListAsync();

        var workloads = new List<KitchenWorkloadDTO>();
        
        foreach (var staff in kitchenStaff)
        {
            var activeOrders = await GetOrdersByStaffAsync(staff.Id);
            var activeOrdersList = activeOrders.Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled).ToList();
            
            workloads.Add(new KitchenWorkloadDTO
            {
                StaffId = staff.Id,
                StaffName = staff.FullName,
                Role = staff.Role,
                Status = staff.Status,
                ActiveOrders = activeOrdersList.Count,
                CompletedOrders = 0, // Would need to track this properly
                AveragePreparationTime = TimeSpan.FromMinutes(30),
                CurrentOrders = activeOrdersList
            });
        }

        return workloads;
    }

    public async Task<KitchenWorkloadDTO?> GetStaffWorkloadAsync(int staffId)
    {
        var workloads = await GetKitchenWorkloadAsync();
        return workloads.FirstOrDefault(w => w.StaffId == staffId);
    }

    public async Task<IEnumerable<Staff>> GetAvailableKitchenStaffAsync()
    {
        return await _context.Staff
            .Where(s => (s.Role == StaffRole.Chef || s.Role == StaffRole.RoomServiceStaff) && 
                       s.Status == StaffStatus.Active)
            .ToListAsync();
    }

    public async Task<OrderSummaryDTO> GetOrderSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.RoomServiceOrders.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(o => o.OrderTime >= startDate.Value);
            
        if (endDate.HasValue)
            query = query.Where(o => o.OrderTime <= endDate.Value);

        var orders = await query.ToListAsync();
        var overdueOrders = await GetOverdueOrdersAsync();

        return new OrderSummaryDTO
        {
            TotalOrders = orders.Count,
            PendingOrders = orders.Count(o => o.Status == OrderStatus.Received),
            PreparingOrders = orders.Count(o => o.Status == OrderStatus.Preparing),
            OutForDeliveryOrders = orders.Count(o => o.Status == OrderStatus.OutForDelivery),
            DeliveredOrders = orders.Count(o => o.Status == OrderStatus.Delivered),
            CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
            OverdueOrders = overdueOrders.Count(),
            TotalRevenue = orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount),
            AveragePreparationTime = TimeSpan.FromMinutes(30),
            AverageDeliveryTime = TimeSpan.FromMinutes(10)
        };
    }

    // Helper methods
    private async Task<OrderDTO?> GetOrderWithItemsAsync(int orderId)
    {
        var order = await _context.RoomServiceOrders
            .Include(o => o.Room)
            .Include(o => o.AssignedStaff)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        return order != null ? MapToOrderDTO(order) : null;
    }

    private static OrderDTO MapToOrderDTO(RoomServiceOrder order)
    {
        return new OrderDTO
        {
            Id = order.Id,
            RoomId = order.RoomId,
            RoomNumber = order.Room.RoomNumber,
            GuestName = order.GuestName,
            BookingId = order.BookingId,
            Status = order.Status,
            OrderTime = order.OrderTime,
            PreparedTime = order.PreparedTime,
            DeliveredTime = order.DeliveredTime,
            TotalAmount = order.TotalAmount,
            SpecialInstructions = order.SpecialInstructions,
            AssignedStaffId = order.AssignedStaffId,
            AssignedStaffName = order.AssignedStaff?.FullName,
            Items = order.Items.Select(i => new OrderItemDTO
            {
                Id = i.Id,
                ItemName = i.ItemName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                Notes = i.Notes
            }).ToList()
        };
    }

    private async Task PublishStatusChangeEvent(RoomServiceOrder order, OrderStatus previousStatus, Staff? staff)
    {
        switch (order.Status)
        {
            case OrderStatus.Preparing when previousStatus != OrderStatus.Preparing:
                var preparingEvent = new OrderPreparingEvent
                {
                    OrderId = order.Id,
                    RoomId = order.RoomId,
                    RoomNumber = order.Room.RoomNumber,
                    ChefId = staff?.Id ?? 0,
                    ChefName = staff?.FullName ?? "Unknown",
                    StartTime = DateTime.UtcNow,
                    EstimatedPreparationTime = TimeSpan.FromMinutes(30)
                };
                await _messageBroker.PublishAsync(preparingEvent, EventTopics.OrderPreparing);
                break;

            case OrderStatus.OutForDelivery when previousStatus != OrderStatus.OutForDelivery:
                var outForDeliveryEvent = new OrderOutForDeliveryEvent
                {
                    OrderId = order.Id,
                    RoomId = order.RoomId,
                    RoomNumber = order.Room.RoomNumber,
                    DeliveryStaffId = staff?.Id ?? 0,
                    DeliveryStaffName = staff?.FullName ?? "Unknown",
                    DeliveryStartTime = DateTime.UtcNow,
                    EstimatedDeliveryTime = TimeSpan.FromMinutes(10)
                };
                await _messageBroker.PublishAsync(outForDeliveryEvent, EventTopics.OrderOutForDelivery);
                break;

            case OrderStatus.Delivered when previousStatus != OrderStatus.Delivered:
                var deliveredEvent = new OrderDeliveredEvent
                {
                    OrderId = order.Id,
                    RoomId = order.RoomId,
                    RoomNumber = order.Room.RoomNumber,
                    DeliveredTime = order.DeliveredTime ?? DateTime.UtcNow,
                    TotalOrderTime = (order.DeliveredTime ?? DateTime.UtcNow) - order.OrderTime,
                    TotalAmount = order.TotalAmount,
                    CustomerSatisfied = true
                };
                await _messageBroker.PublishAsync(deliveredEvent, EventTopics.OrderDelivered);
                break;
        }
    }

    private static IEnumerable<MenuItemDTO> GetSampleMenuItems()
    {
        return new List<MenuItemDTO>
        {
            // Beverages
            new MenuItemDTO { Id = 1, Name = "Coffee", Description = "Fresh brewed coffee", Price = 4.50m, Category = MenuCategory.Beverages, IsAvailable = true, PreparationTimeMinutes = 5 },
            new MenuItemDTO { Id = 2, Name = "Tea", Description = "Selection of premium teas", Price = 3.50m, Category = MenuCategory.Beverages, IsAvailable = true, PreparationTimeMinutes = 5 },
            new MenuItemDTO { Id = 3, Name = "Orange Juice", Description = "Fresh squeezed orange juice", Price = 5.00m, Category = MenuCategory.Beverages, IsAvailable = true, PreparationTimeMinutes = 2 },
            
            // Appetizers
            new MenuItemDTO { Id = 4, Name = "Caesar Salad", Description = "Classic caesar with croutons and parmesan", Price = 12.00m, Category = MenuCategory.Appetizers, IsAvailable = true, PreparationTimeMinutes = 10 },
            new MenuItemDTO { Id = 5, Name = "Chicken Wings", Description = "Spicy buffalo wings with ranch dipping sauce", Price = 14.00m, Category = MenuCategory.Appetizers, IsAvailable = true, PreparationTimeMinutes = 20 },
            
            // Main Course
            new MenuItemDTO { Id = 6, Name = "Grilled Chicken", Description = "Herb marinated grilled chicken breast with vegetables", Price = 24.00m, Category = MenuCategory.MainCourse, IsAvailable = true, PreparationTimeMinutes = 25 },
            new MenuItemDTO { Id = 7, Name = "Beef Steak", Description = "Premium ribeye steak with mashed potatoes", Price = 32.00m, Category = MenuCategory.MainCourse, IsAvailable = true, PreparationTimeMinutes = 30 },
            new MenuItemDTO { Id = 8, Name = "Fish and Chips", Description = "Beer battered fish with crispy fries", Price = 18.00m, Category = MenuCategory.MainCourse, IsAvailable = true, PreparationTimeMinutes = 20 },
            
            // Desserts
            new MenuItemDTO { Id = 9, Name = "Chocolate Cake", Description = "Rich chocolate cake with vanilla ice cream", Price = 8.00m, Category = MenuCategory.Desserts, IsAvailable = true, PreparationTimeMinutes = 5 },
            new MenuItemDTO { Id = 10, Name = "Cheesecake", Description = "New York style cheesecake with berry compote", Price = 9.00m, Category = MenuCategory.Desserts, IsAvailable = true, PreparationTimeMinutes = 5 },
            
            // Breakfast
            new MenuItemDTO { Id = 11, Name = "Continental Breakfast", Description = "Pastries, fruits, coffee and juice", Price = 15.00m, Category = MenuCategory.Breakfast, IsAvailable = true, PreparationTimeMinutes = 10 },
            new MenuItemDTO { Id = 12, Name = "Full English Breakfast", Description = "Eggs, bacon, sausage, beans, toast", Price = 18.00m, Category = MenuCategory.Breakfast, IsAvailable = true, PreparationTimeMinutes = 20 }
        };
    }
}