using BenLanSystem.Data;
using BenLanSystem.Models.DTOs;
using BenLanSystem.Models.Entities;
using BenLanSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BenLanSystem.Services.Implementations;

public class BlogService(ApplicationDbContext db) : IBlogService
{
    public async Task<IEnumerable<BlogPostDto>> GetPublishedAsync(int page = 1, int pageSize = 10)
        => await db.Set<BlogPost>().Include(b => b.Author)
            .Where(b => b.IsPublished)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new BlogPostDto
            {
                Id = b.Id, Title = b.Title, Content = b.Content, Summary = b.Summary,
                ImageUrl = b.ImageUrl, AuthorId = b.AuthorId,
                AuthorName = b.Author.FirstName + " " + b.Author.LastName,
                CreatedAtUtc = b.CreatedAtUtc, UpdatedAtUtc = b.UpdatedAtUtc, IsPublished = b.IsPublished
            }).ToListAsync();

    public async Task<BlogPostDto?> GetByIdAsync(long id)
    {
        var post = await db.Set<BlogPost>().Include(b => b.Author).FirstOrDefaultAsync(b => b.Id == id);
        if (post is null) return null;
        return new BlogPostDto
        {
            Id = post.Id, Title = post.Title, Content = post.Content, Summary = post.Summary,
            ImageUrl = post.ImageUrl, AuthorId = post.AuthorId,
            AuthorName = post.Author.FirstName + " " + post.Author.LastName,
            CreatedAtUtc = post.CreatedAtUtc, UpdatedAtUtc = post.UpdatedAtUtc, IsPublished = post.IsPublished
        };
    }

    public async Task<BlogPostDto> CreateAsync(BlogPostCreateDto dto)
    {
        // AuthorId will be set by the controller from the authenticated user
        var post = new BlogPost
        {
            Title = dto.Title, Content = dto.Content, Summary = dto.Summary,
            ImageUrl = dto.ImageUrl, IsPublished = dto.IsPublished
        };
        db.Set<BlogPost>().Add(post);
        await db.SaveChangesAsync();
        return (await GetByIdAsync(post.Id))!;
    }

    public async Task<BlogPostDto?> UpdateAsync(long id, BlogPostCreateDto dto)
    {
        var post = await db.Set<BlogPost>().FindAsync(id);
        if (post is null) return null;
        post.Title = dto.Title;
        post.Content = dto.Content;
        post.Summary = dto.Summary;
        post.ImageUrl = dto.ImageUrl;
        post.IsPublished = dto.IsPublished;
        post.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var post = await db.Set<BlogPost>().FindAsync(id);
        if (post is null) return false;
        db.Set<BlogPost>().Remove(post);
        await db.SaveChangesAsync();
        return true;
    }
}