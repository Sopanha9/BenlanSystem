using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BenLanSystem.Models.Entities;

public class Staff : IdentityUser<long>
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Position { get; set; }

    [StringLength(20)]
    public string? EmployeeId { get; set; }

    public DateTime? HireDate { get; set; }

    // Navigation properties
    public ICollection<Booking> CustomerBookings { get; set; } = new List<Booking>();
}