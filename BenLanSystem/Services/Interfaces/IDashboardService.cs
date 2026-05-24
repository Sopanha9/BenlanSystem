using BenLanSystem.Models.DTOs;

namespace BenLanSystem.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync();
    Task<IEnumerable<RouteLoadDto>> GetRouteLoadAsync();
    Task<IEnumerable<DashboardAlertDto>> GetAlertsAsync();
}
