using BenLanSystem.Models.DTOs;
using BenLanSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenLanSystem.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Staff")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var summary = await dashboardService.GetSummaryAsync();
        return Ok(summary);
    }

    [HttpGet("route-load")]
    public async Task<ActionResult<IEnumerable<RouteLoadDto>>> GetRouteLoad()
    {
        var routes = await dashboardService.GetRouteLoadAsync();
        return Ok(routes);
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<IEnumerable<DashboardAlertDto>>> GetAlerts()
    {
        var alerts = await dashboardService.GetAlertsAsync();
        return Ok(alerts);
    }
}
