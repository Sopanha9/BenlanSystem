using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.Entities;

public class BlogPost
{
    public long Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Summary { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    [Required]
    public long AuthorId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsPublished { get; set; }

    // Navigation
    public Staff Author { get; set; } = null!;
}