using BenLanSystem.Models.DTOs;

namespace BenLanSystem.Services.Interfaces;

public interface IBlogService
{
    Task<IEnumerable<BlogPostDto>> GetPublishedAsync(int page = 1, int pageSize = 10);
    Task<BlogPostDto?> GetByIdAsync(long id);
    Task<BlogPostDto> CreateAsync(BlogPostCreateDto dto);
    Task<BlogPostDto?> UpdateAsync(long id, BlogPostCreateDto dto);
    Task<bool> DeleteAsync(long id);
}