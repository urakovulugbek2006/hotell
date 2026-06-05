using HotelOS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Shared.Infrastructure;

public class HotelDbContext : DbContext
{
    public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options)
    {
    }

    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<Guest> Guests { get; set; } = null!;
    public DbSet<Booking> Bookings { get; set; } = null!;
    public DbSet<Staff> Staff { get; set; } = null!;
    public DbSet<RoomServiceOrder> RoomServiceOrders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; } = null!;
    public DbSet<Bill> Bills { get; set; } = null!;
    public DbSet<BillLineItem> BillLineItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Room Configuration
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.RoomNumber).IsUnique();
            entity.Property(r => r.RoomNumber).HasMaxLength(10).IsRequired();
            entity.Property(r => r.NightlyRate).HasPrecision(10, 2);
        });

        // Guest Configuration
        modelBuilder.Entity<Guest>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.HasIndex(g => g.Email).IsUnique();
            entity.Property(g => g.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(g => g.LastName).HasMaxLength(100).IsRequired();
            entity.Property(g => g.Email).HasMaxLength(255).IsRequired();
            entity.Property(g => g.PhoneNumber).HasMaxLength(20);
            entity.Property(g => g.PassportNumber).HasMaxLength(20);
            entity.Property(g => g.Nationality).HasMaxLength(50);
        });

        // Booking Configuration
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.HasOne(b => b.Guest).WithMany(g => g.Bookings).HasForeignKey(b => b.GuestId);
            entity.HasOne(b => b.Room).WithMany(r => r.Bookings).HasForeignKey(b => b.RoomId);
            entity.Property(b => b.TotalAmount).HasPrecision(10, 2);
            entity.Property(b => b.PaidAmount).HasPrecision(10, 2);
        });

        // Staff Configuration
        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.Email).IsUnique();
            entity.HasIndex(s => s.EmployeeId).IsUnique();
            entity.Property(s => s.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(s => s.LastName).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Email).HasMaxLength(255).IsRequired();
            entity.Property(s => s.PhoneNumber).HasMaxLength(20);
            entity.Property(s => s.EmployeeId).HasMaxLength(20);
            entity.Property(s => s.HourlyRate).HasPrecision(8, 2);
            entity.Property(s => s.Department).HasMaxLength(100);
            entity.HasOne(s => s.Supervisor).WithMany(s => s.Subordinates).HasForeignKey(s => s.SupervisorId);
        });

        // RoomServiceOrder Configuration
        modelBuilder.Entity<RoomServiceOrder>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasOne(o => o.Room).WithMany(r => r.RoomServiceOrders).HasForeignKey(o => o.RoomId);
            entity.HasOne(o => o.Booking).WithMany().HasForeignKey(o => o.BookingId);
            entity.HasOne(o => o.AssignedStaff).WithMany(s => s.RoomServiceOrders).HasForeignKey(o => o.AssignedStaffId);
            entity.Property(o => o.GuestName).HasMaxLength(200).IsRequired();
            entity.Property(o => o.TotalAmount).HasPrecision(10, 2);
        });

        // OrderItem Configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasOne(i => i.Order).WithMany(o => o.Items).HasForeignKey(i => i.OrderId);
            entity.Property(i => i.ItemName).HasMaxLength(200).IsRequired();
            entity.Property(i => i.UnitPrice).HasPrecision(10, 2);
        });

        // MaintenanceRequest Configuration
        modelBuilder.Entity<MaintenanceRequest>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.HasOne(m => m.Room).WithMany(r => r.MaintenanceRequests).HasForeignKey(m => m.RoomId);
            entity.HasOne(m => m.AssignedTechnician).WithMany(s => s.MaintenanceRequests).HasForeignKey(m => m.AssignedTechnicianId);
            entity.Property(m => m.Description).HasMaxLength(1000).IsRequired();
            entity.Property(m => m.ReportedBy).HasMaxLength(200);
            entity.Property(m => m.EstimatedCost).HasPrecision(10, 2);
            entity.Property(m => m.ActualCost).HasPrecision(10, 2);
        });

        // Bill Configuration
        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.HasOne(b => b.Booking).WithOne(bk => bk.Bill).HasForeignKey<Bill>(b => b.BookingId);
            entity.Property(b => b.RoomCharges).HasPrecision(10, 2);
            entity.Property(b => b.RoomServiceCharges).HasPrecision(10, 2);
            entity.Property(b => b.AdditionalCharges).HasPrecision(10, 2);
            entity.Property(b => b.Taxes).HasPrecision(10, 2);
            entity.Property(b => b.Discounts).HasPrecision(10, 2);
            entity.Property(b => b.TotalAmount).HasPrecision(10, 2);
            entity.Property(b => b.PaidAmount).HasPrecision(10, 2);
            entity.Property(b => b.PaymentReference).HasMaxLength(100);
        });

        // BillLineItem Configuration
        modelBuilder.Entity<BillLineItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasOne(i => i.Bill).WithMany(b => b.LineItems).HasForeignKey(i => i.BillId);
            entity.Property(i => i.Description).HasMaxLength(500).IsRequired();
            entity.Property(i => i.UnitPrice).HasPrecision(10, 2);
        });

        // Seed Data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Rooms (10 rooms across 2 floors as per requirements)
        var rooms = new[]
        {
            new Room { Id = 1, RoomNumber = "101", Floor = 1, Type = RoomType.Single, Status = RoomStatus.Available, NightlyRate = 120.00m, IsAccessible = false, NearElevator = true, NearStairs = false },
            new Room { Id = 2, RoomNumber = "102", Floor = 1, Type = RoomType.Double, Status = RoomStatus.Available, NightlyRate = 180.00m, IsAccessible = false, NearElevator = true, NearStairs = false },
            new Room { Id = 3, RoomNumber = "103", Floor = 1, Type = RoomType.Suite, Status = RoomStatus.Available, NightlyRate = 350.00m, IsAccessible = false, NearElevator = false, NearStairs = true },
            new Room { Id = 4, RoomNumber = "104", Floor = 1, Type = RoomType.Accessible, Status = RoomStatus.Available, NightlyRate = 200.00m, IsAccessible = true, NearElevator = true, NearStairs = false },
            new Room { Id = 5, RoomNumber = "105", Floor = 1, Type = RoomType.Double, Status = RoomStatus.Available, NightlyRate = 180.00m, IsAccessible = false, NearElevator = false, NearStairs = true },
            new Room { Id = 6, RoomNumber = "201", Floor = 2, Type = RoomType.Single, Status = RoomStatus.Available, NightlyRate = 130.00m, IsAccessible = false, NearElevator = true, NearStairs = false },
            new Room { Id = 7, RoomNumber = "202", Floor = 2, Type = RoomType.Double, Status = RoomStatus.Available, NightlyRate = 190.00m, IsAccessible = false, NearElevator = true, NearStairs = false },
            new Room { Id = 8, RoomNumber = "203", Floor = 2, Type = RoomType.Suite, Status = RoomStatus.Available, NightlyRate = 380.00m, IsAccessible = false, NearElevator = false, NearStairs = true },
            new Room { Id = 9, RoomNumber = "204", Floor = 2, Type = RoomType.Double, Status = RoomStatus.Available, NightlyRate = 190.00m, IsAccessible = false, NearElevator = false, NearStairs = true },
            new Room { Id = 10, RoomNumber = "205", Floor = 2, Type = RoomType.Accessible, Status = RoomStatus.Available, NightlyRate = 220.00m, IsAccessible = true, NearElevator = true, NearStairs = false }
        };

        modelBuilder.Entity<Room>().HasData(rooms);

        // Seed Staff
        var staff = new[]
        {
            new Staff { Id = 1, FirstName = "John", LastName = "Manager", Email = "john.manager@hotel.com", Role = StaffRole.Manager, Status = StaffStatus.Active, HireDate = DateTime.UtcNow.AddYears(-2), EmployeeId = "MGR001", Department = "Management" },
            new Staff { Id = 2, FirstName = "Alice", LastName = "Reception", Email = "alice.reception@hotel.com", Role = StaffRole.Receptionist, Status = StaffStatus.Active, HireDate = DateTime.UtcNow.AddYears(-1), EmployeeId = "REC001", Department = "Reception", SupervisorId = 1 },
            new Staff { Id = 3, FirstName = "Bob", LastName = "Housekeeper", Email = "bob.housekeeper@hotel.com", Role = StaffRole.Housekeeper, Status = StaffStatus.Active, HireDate = DateTime.UtcNow.AddYears(-1), EmployeeId = "HSK001", Department = "Housekeeping", SupervisorId = 1 },
            new Staff { Id = 4, FirstName = "Carol", LastName = "Chef", Email = "carol.chef@hotel.com", Role = StaffRole.Chef, Status = StaffStatus.Active, HireDate = DateTime.UtcNow.AddMonths(-8), EmployeeId = "CHF001", Department = "Room Service", SupervisorId = 1 },
            new Staff { Id = 5, FirstName = "Dave", LastName = "Technician", Email = "dave.tech@hotel.com", Role = StaffRole.Technician, Status = StaffStatus.Active, HireDate = DateTime.UtcNow.AddMonths(-6), EmployeeId = "TCH001", Department = "Maintenance", SupervisorId = 1 }
        };

        modelBuilder.Entity<Staff>().HasData(staff);
    }
}