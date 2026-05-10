using BenLanSystem.Models.DTOs;
using BenLanSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenLanSystem.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class BlogController(IBlogService blogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BlogPostDto>>> GetPublished([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var posts = await blogService.GetPublishedAsync(page, pageSize);
        return Ok(posts);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<BlogPostDto>> GetById(long id)
    {
        var post = await blogService.GetByIdAsync(id);
        if (post is null) return NotFound();
        return Ok(post);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<BlogPostDto>> Create(BlogPostCreateDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        var post = await blogService.CreateAsync(dto, long.Parse(userId));
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<BlogPostDto>> Update(long id, BlogPostCreateDto dto)
    {
        var post = await blogService.UpdateAsync(id, dto);
        if (post is null) return NotFound();
        return Ok(post);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(long id)
    {
        var deleted = await blogService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
