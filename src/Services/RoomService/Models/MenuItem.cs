using RoomService.DTOs;
using System.ComponentModel.DataAnnotations;

namespace RoomService.Models;

public class MenuItem
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Range(0.01, 1000.00)]
    public decimal Price { get; set; }
    
    public MenuCategory Category { get; set; }
    
    public bool IsAvailable { get; set; } = true;
    
    public string? ImageUrl { get; set; }
    
    public int PreparationTimeMinutes { get; set; } = 30;
    
    public List<string> Allergens { get; set; } = new List<string>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}