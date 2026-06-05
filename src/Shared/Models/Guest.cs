using System.ComponentModel.DataAnnotations;

namespace HotelOS.Shared.Models;

public class Guest
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    // Hashed password (SHA-256). Never store plain-text passwords.
    public string? PasswordHash { get; set; }
    
    [Phone]
    public string? PhoneNumber { get; set; }
    
    public string? Address { get; set; }
    
    public DateTime DateOfBirth { get; set; }
    
    public string? PassportNumber { get; set; }
    
    public string? Nationality { get; set; }
    
    public bool IsVip { get; set; }
    
    public string? SpecialRequests { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    
    // Computed property
    public string FullName => $"{FirstName} {LastName}";
}