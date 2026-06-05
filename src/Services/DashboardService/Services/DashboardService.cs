using DashboardService.DTOs;
using DashboardService.Hubs;
using HotelOS.Shared.Infrastructure;
using HotelOS.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DashboardService.Services;

public class DashboardService : IDashboardService
{
    private readonly HotelDbContext _context;
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        HotelDbContext context,
        IHubContext<DashboardHub> hubContext,
        ILogger<DashboardService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HotelOverviewDTO> GetHotelOverviewAsync()
    {
        try
        {
            var totalRooms = await _context.Rooms.CountAsync();
            var occupiedRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied);
            var availableRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Available || r.Status == RoomStatus.Clean);
            var dirtyRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Dirty);
            var maintenanceRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Maintenance || r.Status == RoomStatus.OutOfOrder);

            var activeGuests = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.CheckedIn);
            var pendingOrders = await _context.RoomServiceOrders.CountAsync(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled);
            var openMaintenance = await _context.MaintenanceRequests.CountAsync(m => m.Status != MaintenanceStatus.Completed && m.Status != MaintenanceStatus.Cancelled);
            var activeStaff = await _context.Staff.CountAsync(s => s.Status == StaffStatus.Active || s.Status == StaffStatus.OnShift);

            // Calculate today's revenue
            var today = DateTime.UtcNow.Date;
            var todaysRevenue = await _context.Bills
                .Where(b => b.CreatedAt >= today && b.Status == BillStatus.Paid)
                .SumAsync(b => b.TotalAmount);

            return new HotelOverviewDTO
            {
                TotalRooms = totalRooms,
                OccupiedRooms = occupiedRooms,
                AvailableRooms = availableRooms,
                DirtyRooms = dirtyRooms,
                MaintenanceRooms = maintenanceRooms,
                OccupancyRate = totalRooms > 0 ? (decimal)occupiedRooms / totalRooms * 100 : 0,
                TodaysRevenue = todaysRevenue,
                ActiveGuests = activeGuests,
                PendingOrders = pendingOrders,
                OpenMaintenanceRequests = openMaintenance,
                ActiveStaff = activeStaff,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel overview");
            throw;
        }
    }

    public async Task<RoomStatusDashboardDTO> GetRoomStatusDashboardAsync()
    {
        try
        {
            var rooms = await _context.Rooms
                .Include(r => r.Bookings.Where(b => b.Status == BookingStatus.CheckedIn))
                .ThenInclude(b => b.Guest)
                .OrderBy(r => r.Floor)
                .ThenBy(r => r.RoomNumber)
                .ToListAsync();

            var roomItems = rooms.Select(room => MapToRoomStatusItem(room)).ToList();

            var summary = new RoomStatusSummaryDTO
            {
                Available = rooms.Count(r => r.Status == RoomStatus.Available),
                Occupied = rooms.Count(r => r.Status == RoomStatus.Occupied),
                Dirty = rooms.Count(r => r.Status == RoomStatus.Dirty),
                BeingCleaned = rooms.Count(r => r.Status == RoomStatus.BeingCleaned),
                Clean = rooms.Count(r => r.Status == RoomStatus.Clean),
                Maintenance = rooms.Count(r => r.Status == RoomStatus.Maintenance),
                OutOfOrder = rooms.Count(r => r.Status == RoomStatus.OutOfOrder)
            };

            return new RoomStatusDashboardDTO
            {
                Rooms = roomItems,
                Summary = summary,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room status dashboard");
            throw;
        }
    }

    public async Task<List<ActiveBookingDashboardDTO>> GetActiveBookingsAsync()
    {
        try
        {
            var activeBookings = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .Where(b => b.Status == BookingStatus.Confirmed || 
                           b.Status == BookingStatus.CheckedIn || 
                           b.Status == BookingStatus.Pending)
                .OrderBy(b => b.CheckInDate)
                .ToListAsync();

            return activeBookings.Select(MapToActiveBookingDashboard).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active bookings");
            throw;
        }
    }

    public async Task<List<OrderDashboardDTO>> GetActiveOrdersAsync()
    {
        try
        {
            var activeOrders = await _context.RoomServiceOrders
                .Include(o => o.Room)
                .Include(o => o.AssignedStaff)
                .Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled)
                .OrderBy(o => o.OrderTime)
                .ToListAsync();

            return activeOrders.Select(MapToOrderDashboard).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active orders");
            throw;
        }
    }

    public async Task<List<MaintenanceDashboardDTO>> GetActiveMaintenanceAsync()
    {
        try
        {
            var activeRequests = await _context.MaintenanceRequests
                .Include(r => r.Room)
                .Include(r => r.AssignedTechnician)
                .Where(r => r.Status != MaintenanceStatus.Completed && r.Status != MaintenanceStatus.Cancelled)
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.ReportedAt)
                .ToListAsync();

            var dashboardItems = new List<MaintenanceDashboardDTO>();
            for (int i = 0; i < activeRequests.Count; i++)
            {
                var item = MapToMaintenanceDashboard(activeRequests[i]);
                item.QueuePosition = i + 1;
                dashboardItems.Add(item);
            }

            return dashboardItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active maintenance");
            throw;
        }
    }

    public async Task<List<StaffWorkloadDTO>> GetStaffWorkloadsAsync()
    {
        try
        {
            var staff = await _context.Staff
                .Where(s => s.Status == StaffStatus.Active || s.Status == StaffStatus.OnShift)
                .ToListAsync();

            var workloads = new List<StaffWorkloadDTO>();
            
            foreach (var member in staff)
            {
                var activeTasks = await GetActiveTasksForStaff(member.Id, member.Role);
                var completedToday = await GetCompletedTasksToday(member.Id, member.Role);
                
                workloads.Add(MapToStaffWorkload(member, activeTasks, completedToday));
            }

            return workloads.OrderBy(w => w.Department).ThenBy(w => w.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting staff workloads");
            throw;
        }
    }

    public async Task<OccupancyMetricsDTO> GetOccupancyMetricsAsync()
    {
        try
        {
            var totalRooms = await _context.Rooms.CountAsync();
            var occupiedRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied);
            
            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var checkInsToday = await _context.Bookings
                .CountAsync(b => b.ActualCheckIn.HasValue && b.ActualCheckIn.Value.Date == today);
            
            var checkOutsToday = await _context.Bookings
                .CountAsync(b => b.ActualCheckOut.HasValue && b.ActualCheckOut.Value.Date == today);

            var arrivalsExpected = await _context.Bookings
                .CountAsync(b => b.CheckInDate.Date == today && b.Status == BookingStatus.Confirmed);

            var departuresExpected = await _context.Bookings
                .CountAsync(b => b.CheckOutDate.Date == today && b.Status == BookingStatus.CheckedIn);

            return new OccupancyMetricsDTO
            {
                CurrentOccupancyRate = totalRooms > 0 ? (decimal)occupiedRooms / totalRooms * 100 : 0,
                WeeklyAverageOccupancy = await CalculateWeeklyAverageOccupancy(weekStart),
                MonthlyAverageOccupancy = await CalculateMonthlyAverageOccupancy(monthStart),
                CheckInsToday = checkInsToday,
                CheckOutsToday = checkOutsToday,
                ArrivalsExpected = arrivalsExpected,
                DeparturesExpected = departuresExpected,
                HourlyTrends = await GetHourlyOccupancyTrends(today)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting occupancy metrics");
            throw;
        }
    }

    public async Task<RevenueMetricsDTO> GetRevenueMetricsAsync()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var todaysRevenue = await _context.Bills
                .Where(b => b.CreatedAt >= today && b.Status == BillStatus.Paid)
                .SumAsync(b => b.TotalAmount);

            var weeklyRevenue = await _context.Bills
                .Where(b => b.CreatedAt >= weekStart && b.Status == BillStatus.Paid)
                .SumAsync(b => b.TotalAmount);

            var monthlyRevenue = await _context.Bills
                .Where(b => b.CreatedAt >= monthStart && b.Status == BillStatus.Paid)
                .SumAsync(b => b.TotalAmount);

            var roomServiceRevenue = await _context.Bills
                .Where(b => b.CreatedAt >= today && b.Status == BillStatus.Paid)
                .SumAsync(b => b.RoomServiceCharges);

            var additionalServicesRevenue = await _context.Bills
                .Where(b => b.CreatedAt >= today && b.Status == BillStatus.Paid)
                .SumAsync(b => b.AdditionalCharges);

            var averageRoomRate = await CalculateAverageRoomRate();
            var totalRooms = await _context.Rooms.CountAsync();

            return new RevenueMetricsDTO
            {
                TodaysRevenue = todaysRevenue,
                WeeklyRevenue = weeklyRevenue,
                MonthlyRevenue = monthlyRevenue,
                TargetDailyRevenue = 15000m, // This would be configurable
                AverageRoomRate = averageRoomRate,
                RevenuePAR = totalRooms > 0 ? todaysRevenue / totalRooms : 0,
                RoomServiceRevenue = roomServiceRevenue,
                AdditionalServicesRevenue = additionalServicesRevenue,
                CategoryBreakdown = await GetRevenueBreakdown(today)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue metrics");
            throw;
        }
    }

    public async Task<PerformanceMetricsDTO> GetPerformanceMetricsAsync()
    {
        try
        {
            return new PerformanceMetricsDTO
            {
                AverageCheckInTime = TimeSpan.FromMinutes(8), // Would calculate from actual data
                AverageCheckOutTime = TimeSpan.FromMinutes(5),
                AverageRoomServiceTime = TimeSpan.FromMinutes(32),
                AverageMaintenanceResponseTime = TimeSpan.FromMinutes(45),
                AverageCleaningTime = TimeSpan.FromMinutes(35),
                GuestSatisfactionScore = 4.5m,
                StaffEfficiencyScore = 87.5m,
                ServiceRequests = await _context.MaintenanceRequests.CountAsync(r => r.ReportedAt >= DateTime.UtcNow.Date),
                ResolvedRequests = await _context.MaintenanceRequests.CountAsync(r => r.CompletedAt >= DateTime.UtcNow.Date)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics");
            throw;
        }
    }

    public async Task<AlertsDTO> GetSystemAlertsAsync()
    {
        try
        {
            var alerts = new List<SystemAlert>();
            
            // Check for overdue maintenance requests
            var overdueMaintenanceCount = await _context.MaintenanceRequests
                .CountAsync(r => r.Status != MaintenanceStatus.Completed && 
                           r.ReportedAt < DateTime.UtcNow.AddHours(-2));
            
            if (overdueMaintenanceCount > 0)
            {
                alerts.Add(new SystemAlert
                {
                    Type = AlertType.Maintenance,
                    Severity = AlertSeverity.Warning,
                    Title = "Overdue Maintenance Requests",
                    Message = $"{overdueMaintenanceCount} maintenance request(s) are overdue",
                    IconClass = "fas fa-wrench",
                    ColorClass = "text-warning",
                    RequiresAction = true
                });
            }

            // Check for rooms out of order
            var outOfOrderRooms = await _context.Rooms
                .Where(r => r.Status == RoomStatus.OutOfOrder)
                .ToListAsync();
            
            foreach (var room in outOfOrderRooms)
            {
                alerts.Add(new SystemAlert
                {
                    Type = AlertType.RoomStatus,
                    Severity = AlertSeverity.Critical,
                    Title = "Room Out of Order",
                    Message = $"Room {room.RoomNumber} is currently out of order",
                    RoomNumber = room.RoomNumber,
                    IconClass = "fas fa-exclamation-triangle",
                    ColorClass = "text-danger",
                    RequiresAction = true
                });
            }

            // Check for overdue room service orders
            var overdueOrders = await _context.RoomServiceOrders
                .Include(o => o.Room)
                .Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled &&
                           o.OrderTime < DateTime.UtcNow.AddMinutes(-45))
                .ToListAsync();

            foreach (var order in overdueOrders)
            {
                alerts.Add(new SystemAlert
                {
                    Type = AlertType.RoomService,
                    Severity = AlertSeverity.Warning,
                    Title = "Overdue Room Service Order",
                    Message = $"Order #{order.Id} for room {order.Room.RoomNumber} is overdue",
                    RoomNumber = order.Room.RoomNumber,
                    IconClass = "fas fa-clock",
                    ColorClass = "text-warning",
                    RequiresAction = true
                });
            }

            // Check for critical maintenance issues
            var criticalMaintenance = await _context.MaintenanceRequests
                .Include(r => r.Room)
                .Where(r => r.Priority == MaintenancePriority.Critical && 
                           r.Status == MaintenanceStatus.Reported)
                .ToListAsync();

            foreach (var request in criticalMaintenance)
            {
                alerts.Add(new SystemAlert
                {
                    Type = AlertType.Maintenance,
                    Severity = AlertSeverity.Critical,
                    Title = "Critical Maintenance Required",
                    Message = $"Critical issue in room {request.Room.RoomNumber}: {request.Description}",
                    RoomNumber = request.Room.RoomNumber,
                    IconClass = "fas fa-exclamation-circle",
                    ColorClass = "text-danger",
                    RequiresAction = true
                });
            }

            return new AlertsDTO
            {
                CriticalAlerts = alerts.Where(a => a.Severity == AlertSeverity.Critical).ToList(),
                WarningAlerts = alerts.Where(a => a.Severity == AlertSeverity.Warning).ToList(),
                InfoAlerts = alerts.Where(a => a.Severity == AlertSeverity.Info).ToList(),
                TotalActiveAlerts = alerts.Count,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system alerts");
            throw;
        }
    }

    // Real-time broadcasting methods
    public async Task BroadcastRoomStatusUpdateAsync(int roomId, RoomStatus newStatus)
    {
        try
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room != null)
            {
                var update = new DashboardUpdateDTO
                {
                    Type = DashboardUpdateType.RoomStatusChanged,
                    Message = $"Room {room.RoomNumber} status changed to {newStatus}",
                    Data = new { RoomId = roomId, RoomNumber = room.RoomNumber, NewStatus = newStatus },
                    RoomNumber = room.RoomNumber,
                    RequiresRefresh = true
                };

                await _hubContext.Clients.Group("Dashboard").SendAsync("RoomStatusUpdate", update);
                await _hubContext.Clients.Group($"Room-{room.RoomNumber}").SendAsync("RoomUpdate", update);
                
                _logger.LogInformation("Broadcasted room status update for room {RoomNumber}", room.RoomNumber);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting room status update for room {RoomId}", roomId);
        }
    }

    public async Task BroadcastOrderUpdateAsync(int orderId, OrderStatus newStatus)
    {
        try
        {
            var order = await _context.RoomServiceOrders
                .Include(o => o.Room)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            
            if (order != null)
            {
                var update = new DashboardUpdateDTO
                {
                    Type = DashboardUpdateType.OrderStatusChanged,
                    Message = $"Order #{orderId} for room {order.Room.RoomNumber} is now {newStatus}",
                    Data = new { OrderId = orderId, RoomNumber = order.Room.RoomNumber, NewStatus = newStatus },
                    RoomNumber = order.Room.RoomNumber
                };

                await _hubContext.Clients.Group("Dashboard").SendAsync("OrderUpdate", update);
                
                _logger.LogInformation("Broadcasted order update for order {OrderId}", orderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting order update for order {OrderId}", orderId);
        }
    }

    public async Task BroadcastMaintenanceUpdateAsync(int requestId, MaintenanceStatus newStatus)
    {
        try
        {
            var request = await _context.MaintenanceRequests
                .Include(r => r.Room)
                .FirstOrDefaultAsync(r => r.Id == requestId);
            
            if (request != null)
            {
                var update = new DashboardUpdateDTO
                {
                    Type = DashboardUpdateType.MaintenanceStatusChanged,
                    Message = $"Maintenance request #{requestId} for room {request.Room.RoomNumber} is now {newStatus}",
                    Data = new { RequestId = requestId, RoomNumber = request.Room.RoomNumber, NewStatus = newStatus },
                    RoomNumber = request.Room.RoomNumber
                };

                await _hubContext.Clients.Group("Dashboard").SendAsync("MaintenanceUpdate", update);
                
                _logger.LogInformation("Broadcasted maintenance update for request {RequestId}", requestId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting maintenance update for request {RequestId}", requestId);
        }
    }

    public async Task BroadcastCheckInOutAsync(string eventType, int roomId, string guestName)
    {
        try
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room != null)
            {
                var updateType = eventType == "CheckIn" ? DashboardUpdateType.GuestCheckedIn : DashboardUpdateType.GuestCheckedOut;
                
                var update = new DashboardUpdateDTO
                {
                    Type = updateType,
                    Message = $"Guest {guestName} {eventType.ToLower()} room {room.RoomNumber}",
                    Data = new { RoomId = roomId, RoomNumber = room.RoomNumber, GuestName = guestName, EventType = eventType },
                    RoomNumber = room.RoomNumber,
                    RequiresRefresh = true
                };

                await _hubContext.Clients.Group("Dashboard").SendAsync("CheckInOutUpdate", update);
                
                _logger.LogInformation("Broadcasted {EventType} update for room {RoomNumber}", eventType, room.RoomNumber);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting {EventType} update for room {RoomId}", eventType, roomId);
        }
    }

    public async Task BroadcastSystemAlertAsync(SystemAlert alert)
    {
        try
        {
            var update = new DashboardUpdateDTO
            {
                Type = DashboardUpdateType.SystemAlert,
                Message = alert.Message,
                Data = alert
            };

            await _hubContext.Clients.Group("Dashboard").SendAsync("SystemAlert", update);
            
            _logger.LogInformation("Broadcasted system alert: {AlertTitle}", alert.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting system alert: {AlertTitle}", alert.Title);
        }
    }

    // Historical data methods
    public async Task<List<OccupancyTrendDTO>> GetOccupancyTrendsAsync(DateTime startDate, DateTime endDate)
    {
        // Implementation would aggregate historical occupancy data
        // For demo purposes, returning sample data
        var trends = new List<OccupancyTrendDTO>();
        var totalRooms = await _context.Rooms.CountAsync();
        
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            trends.Add(new OccupancyTrendDTO
            {
                Date = date,
                OccupancyRate = Random.Shared.Next(60, 95), // Sample data
                TotalRooms = totalRooms,
                OccupiedRooms = Random.Shared.Next(6, 10),
                Revenue = Random.Shared.Next(8000, 15000),
                AverageRate = Random.Shared.Next(150, 250)
            });
        }
        
        return trends;
    }

    public async Task<List<RevenueBreakdownDTO>> GetRevenueBreakdownAsync(DateTime startDate, DateTime endDate)
    {
        var bills = await _context.Bills
            .Where(b => b.CreatedAt >= startDate && b.CreatedAt <= endDate && b.Status == BillStatus.Paid)
            .ToListAsync();

        var totalRevenue = bills.Sum(b => b.TotalAmount);
        
        return new List<RevenueBreakdownDTO>
        {
            new() { Category = "Room Charges", Amount = bills.Sum(b => b.RoomCharges), Percentage = totalRevenue > 0 ? bills.Sum(b => b.RoomCharges) / totalRevenue * 100 : 0, Color = "#007bff" },
            new() { Category = "Room Service", Amount = bills.Sum(b => b.RoomServiceCharges), Percentage = totalRevenue > 0 ? bills.Sum(b => b.RoomServiceCharges) / totalRevenue * 100 : 0, Color = "#28a745" },
            new() { Category = "Additional Services", Amount = bills.Sum(b => b.AdditionalCharges), Percentage = totalRevenue > 0 ? bills.Sum(b => b.AdditionalCharges) / totalRevenue * 100 : 0, Color = "#ffc107" },
            new() { Category = "Taxes", Amount = bills.Sum(b => b.Taxes), Percentage = totalRevenue > 0 ? bills.Sum(b => b.Taxes) / totalRevenue * 100 : 0, Color = "#dc3545" }
        };
    }

    public async Task<List<ServiceMetricsDTO>> GetServiceMetricsAsync(DateTime startDate, DateTime endDate)
    {
        // Implementation would calculate actual service metrics
        // For demo purposes, returning sample data
        var metrics = new List<ServiceMetricsDTO>();
        
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            metrics.Add(new ServiceMetricsDTO
            {
                Date = date,
                RoomServiceOrders = Random.Shared.Next(15, 35),
                AverageDeliveryTime = TimeSpan.FromMinutes(Random.Shared.Next(25, 45)),
                MaintenanceRequests = Random.Shared.Next(3, 12),
                AverageResolutionTime = TimeSpan.FromMinutes(Random.Shared.Next(30, 90)),
                CustomerSatisfactionScore = (decimal)(Random.Shared.NextDouble() * 1.5 + 3.5),
                StaffUtilizationRate = Random.Shared.Next(75, 95)
            });
        }
        
        return metrics;
    }

    // Helper mapping methods
    private static RoomStatusItemDTO MapToRoomStatusItem(Room room)
    {
        var currentBooking = room.Bookings.FirstOrDefault();
        
        return new RoomStatusItemDTO
        {
            Id = room.Id,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            Type = room.Type,
            Status = room.Status,
            CurrentGuestName = currentBooking?.Guest?.FullName,
            CheckInTime = currentBooking?.ActualCheckIn,
            ExpectedCheckOut = currentBooking?.CheckOutDate,
            LastCleaned = room.LastCleaned,
            HasActiveOrders = false, // Would need to check room service orders
            HasMaintenanceIssues = false, // Would need to check maintenance requests
            StatusColor = GetRoomStatusColor(room.Status),
            MinutesSinceStatusChange = null // Would track status change time
        };
    }

    private static ActiveBookingDashboardDTO MapToActiveBookingDashboard(Booking booking)
    {
        var nightsRemaining = booking.Status == BookingStatus.CheckedIn 
            ? Math.Max(0, (booking.CheckOutDate - DateTime.UtcNow.Date).Days)
            : (booking.CheckOutDate - booking.CheckInDate).Days;
        
        return new ActiveBookingDashboardDTO
        {
            Id = booking.Id,
            GuestName = booking.Guest.FullName,
            RoomNumber = booking.Room?.RoomNumber,
            Status = booking.Status,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            ActualCheckIn = booking.ActualCheckIn,
            RoomType = booking.RequestedRoomType,
            TotalAmount = booking.TotalAmount,
            NightsRemaining = nightsRemaining,
            IsOverdue = booking.CheckInDate < DateTime.UtcNow.Date && booking.Status == BookingStatus.Confirmed,
            StatusBadge = GetBookingStatusBadge(booking.Status)
        };
    }

    private static OrderDashboardDTO MapToOrderDashboard(RoomServiceOrder order)
    {
        var elapsedTime = DateTime.UtcNow - order.OrderTime;
        var isOverdue = elapsedTime > TimeSpan.FromMinutes(45) && order.Status != OrderStatus.Delivered;
        
        return new OrderDashboardDTO
        {
            Id = order.Id,
            RoomNumber = order.Room.RoomNumber,
            GuestName = order.GuestName,
            Status = order.Status,
            OrderTime = order.OrderTime,
            TotalAmount = order.TotalAmount,
            ItemCount = order.Items.Count,
            AssignedStaffName = order.AssignedStaff?.FullName,
            ElapsedTime = elapsedTime,
            EstimatedCompletionTime = GetEstimatedCompletionTime(order.Status, order.OrderTime),
            IsOverdue = isOverdue,
            StatusColor = GetOrderStatusColor(order.Status),
            PriorityLevel = isOverdue ? "High" : "Normal"
        };
    }

    private static MaintenanceDashboardDTO MapToMaintenanceDashboard(MaintenanceRequest request)
    {
        var elapsedTime = DateTime.UtcNow - request.ReportedAt;
        var estimatedTime = GetMaintenanceEstimatedTime(request.Priority);
        var isOverdue = elapsedTime > estimatedTime;
        
        return new MaintenanceDashboardDTO
        {
            Id = request.Id,
            RoomNumber = request.Room.RoomNumber,
            Description = request.Description.Length > 50 ? request.Description[..50] + "..." : request.Description,
            Priority = request.Priority,
            Status = request.Status,
            ReportedAt = request.ReportedAt,
            AssignedTechnicianName = request.AssignedTechnician?.FullName,
            ElapsedTime = elapsedTime,
            EstimatedResolutionTime = estimatedTime,
            IsOverdue = isOverdue,
            PriorityColor = GetMaintenancePriorityColor(request.Priority),
            StatusBadge = GetMaintenanceStatusBadge(request.Status)
        };
    }

    private static StaffWorkloadDTO MapToStaffWorkload(Staff staff, int activeTasks, int completedToday)
    {
        var workloadLevel = activeTasks switch
        {
            0 => "Available",
            1 => "Light",
            2 => "Moderate",
            3 => "Heavy",
            _ => "Overloaded"
        };

        return new StaffWorkloadDTO
        {
            Id = staff.Id,
            Name = staff.FullName,
            Role = staff.Role,
            Status = staff.Status,
            ActiveTasks = activeTasks,
            CompletedTasksToday = completedToday,
            Department = staff.Department ?? GetDepartmentByRole(staff.Role),
            ShiftDuration = TimeSpan.FromHours(8), // Default shift
            CurrentTask = GetCurrentTask(staff.Role, activeTasks),
            WorkloadLevel = workloadLevel,
            StatusColor = GetStaffStatusColor(staff.Status)
        };
    }

    // Helper methods for styling and calculations
    private static string GetRoomStatusColor(RoomStatus status) => status switch
    {
        RoomStatus.Available => "success",
        RoomStatus.Occupied => "primary",
        RoomStatus.Dirty => "warning",
        RoomStatus.BeingCleaned => "info",
        RoomStatus.Clean => "success",
        RoomStatus.Maintenance => "danger",
        RoomStatus.OutOfOrder => "dark",
        _ => "secondary"
    };

    private static string GetBookingStatusBadge(BookingStatus status) => status switch
    {
        BookingStatus.Pending => "warning",
        BookingStatus.Confirmed => "info",
        BookingStatus.CheckedIn => "success",
        BookingStatus.CheckedOut => "secondary",
        BookingStatus.Cancelled => "danger",
        _ => "secondary"
    };

    private static string GetOrderStatusColor(OrderStatus status) => status switch
    {
        OrderStatus.Received => "info",
        OrderStatus.Preparing => "warning",
        OrderStatus.OutForDelivery => "primary",
        OrderStatus.Delivered => "success",
        OrderStatus.Cancelled => "danger",
        _ => "secondary"
    };

    private static string GetMaintenancePriorityColor(MaintenancePriority priority) => priority switch
    {
        MaintenancePriority.Low => "success",
        MaintenancePriority.Normal => "info",
        MaintenancePriority.High => "warning",
        MaintenancePriority.Critical => "danger",
        _ => "secondary"
    };

    private static string GetMaintenanceStatusBadge(MaintenanceStatus status) => status switch
    {
        MaintenanceStatus.Reported => "warning",
        MaintenanceStatus.Assigned => "info",
        MaintenanceStatus.InProgress => "primary",
        MaintenanceStatus.Completed => "success",
        MaintenanceStatus.Cancelled => "danger",
        _ => "secondary"
    };

    private static string GetStaffStatusColor(StaffStatus status) => status switch
    {
        StaffStatus.Active => "success",
        StaffStatus.OnShift => "primary",
        StaffStatus.OnBreak => "warning",
        StaffStatus.OffDuty => "secondary",
        _ => "secondary"
    };

    private static string GetDepartmentByRole(StaffRole role) => role switch
    {
        StaffRole.Manager => "Management",
        StaffRole.Receptionist => "Front Desk",
        StaffRole.Housekeeper or StaffRole.HousekeepingSupervisor => "Housekeeping",
        StaffRole.Chef or StaffRole.RoomServiceStaff => "Food & Beverage",
        StaffRole.Technician or StaffRole.MaintenanceSupervisor => "Maintenance",
        StaffRole.Security => "Security",
        _ => "General"
    };

    private static string GetCurrentTask(StaffRole role, int activeTasks) => role switch
    {
        StaffRole.Receptionist when activeTasks > 0 => "Assisting guests",
        StaffRole.Housekeeper when activeTasks > 0 => "Cleaning rooms",
        StaffRole.Chef when activeTasks > 0 => "Preparing orders",
        StaffRole.Technician when activeTasks > 0 => "Maintenance work",
        _ when activeTasks > 0 => "Working on tasks",
        _ => "Available"
    };

    private static TimeSpan? GetEstimatedCompletionTime(OrderStatus status, DateTime orderTime) => status switch
    {
        OrderStatus.Received => TimeSpan.FromMinutes(35),
        OrderStatus.Preparing => TimeSpan.FromMinutes(20),
        OrderStatus.OutForDelivery => TimeSpan.FromMinutes(8),
        _ => null
    };

    private static TimeSpan GetMaintenanceEstimatedTime(MaintenancePriority priority) => priority switch
    {
        MaintenancePriority.Critical => TimeSpan.FromMinutes(15),
        MaintenancePriority.High => TimeSpan.FromHours(2),
        MaintenancePriority.Normal => TimeSpan.FromHours(8),
        MaintenancePriority.Low => TimeSpan.FromHours(24),
        _ => TimeSpan.FromHours(4)
    };

    // Data calculation helper methods
    private async Task<int> GetActiveTasksForStaff(int staffId, StaffRole role)
    {
        return role switch
        {
            StaffRole.Housekeeper => await _context.Rooms.CountAsync(r => r.Status == RoomStatus.BeingCleaned),
            StaffRole.Chef or StaffRole.RoomServiceStaff => await _context.RoomServiceOrders
                .CountAsync(o => o.AssignedStaffId == staffId && o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled),
            StaffRole.Technician => await _context.MaintenanceRequests
                .CountAsync(r => r.AssignedTechnicianId == staffId && r.Status == MaintenanceStatus.InProgress),
            _ => 0
        };
    }

    private async Task<int> GetCompletedTasksToday(int staffId, StaffRole role)
    {
        var today = DateTime.UtcNow.Date;
        return role switch
        {
            StaffRole.RoomServiceStaff or StaffRole.Chef => await _context.RoomServiceOrders
                .CountAsync(o => o.AssignedStaffId == staffId && o.DeliveredTime >= today),
            StaffRole.Technician => await _context.MaintenanceRequests
                .CountAsync(r => r.AssignedTechnicianId == staffId && r.CompletedAt >= today),
            _ => 0
        };
    }

    private async Task<decimal> CalculateWeeklyAverageOccupancy(DateTime weekStart)
    {
        // Sample calculation - would implement proper historical data aggregation
        var totalRooms = await _context.Rooms.CountAsync();
        return totalRooms > 0 ? 78.5m : 0;
    }

    private async Task<decimal> CalculateMonthlyAverageOccupancy(DateTime monthStart)
    {
        // Sample calculation - would implement proper historical data aggregation
        var totalRooms = await _context.Rooms.CountAsync();
        return totalRooms > 0 ? 82.3m : 0;
    }

    private async Task<List<HourlyOccupancyDTO>> GetHourlyOccupancyTrends(DateTime date)
    {
        // Sample data - would implement proper hourly tracking
        var trends = new List<HourlyOccupancyDTO>();
        var totalRooms = await _context.Rooms.CountAsync();
        
        for (int hour = 0; hour < 24; hour++)
        {
            trends.Add(new HourlyOccupancyDTO
            {
                Hour = date.AddHours(hour),
                OccupancyRate = Random.Shared.Next(60, 95),
                CheckIns = hour >= 14 && hour <= 18 ? Random.Shared.Next(0, 3) : 0,
                CheckOuts = hour >= 8 && hour <= 12 ? Random.Shared.Next(0, 2) : 0
            });
        }
        
        return trends;
    }

    private async Task<decimal> CalculateAverageRoomRate()
    {
        return await _context.Rooms.AverageAsync(r => r.NightlyRate);
    }

    private async Task<List<RevenueBreakdownDTO>> GetRevenueBreakdown(DateTime date)
    {
        var bills = await _context.Bills
            .Where(b => b.CreatedAt >= date && b.Status == BillStatus.Paid)
            .ToListAsync();

        if (!bills.Any()) return new List<RevenueBreakdownDTO>();

        var total = bills.Sum(b => b.TotalAmount);
        return new List<RevenueBreakdownDTO>
        {
            new() { Category = "Room Revenue", Amount = bills.Sum(b => b.RoomCharges), Percentage = bills.Sum(b => b.RoomCharges) / total * 100, Color = "#007bff" },
            new() { Category = "Food & Beverage", Amount = bills.Sum(b => b.RoomServiceCharges), Percentage = bills.Sum(b => b.RoomServiceCharges) / total * 100, Color = "#28a745" },
            new() { Category = "Other Services", Amount = bills.Sum(b => b.AdditionalCharges), Percentage = bills.Sum(b => b.AdditionalCharges) / total * 100, Color = "#ffc107" }
        };
    }
}