using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.DTOs;

public class StaffDto
{
    public long Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? EmployeeId { get; set; }
    public DateTime? HireDate { get; set; }
    public string? PhoneNumber { get; set; }
}

public class StaffCreateDto
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Position { get; set; }

    [StringLength(20)]
    public string? EmployeeId { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }
}

public class StaffUpdateDto
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [StringLength(20)]
    public string? Position { get; set; }

    [StringLength(20)]
    public string? EmployeeId { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }
}