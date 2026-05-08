using System.ComponentModel.DataAnnotations;

namespace BenLanSystem.Models.DTOs;

public class BlogPostDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? ImageUrl { get; set; }
    public long AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsPublished { get; set; }
}

public class BlogPostCreateDto
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Summary { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsPublished { get; set; }
}