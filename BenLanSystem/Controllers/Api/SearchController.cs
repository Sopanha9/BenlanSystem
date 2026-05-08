using BenLanSystem.Models.DTOs;
using BenLanSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BenLanSystem.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class SearchController(ITripService tripService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<TripSearchResultDto>>> Search([FromQuery] TripSearchDto search)
    {
        var result = await tripService.SearchAsync(search);
        return Ok(result);
    }
}