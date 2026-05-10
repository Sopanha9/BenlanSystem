using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenLanSystem.Controllers.Admin;

[Authorize(Roles = "Admin,Staff")]
[Area("Admin")]
public class VehiclesController : Controller
{
    public IActionResult Index() => View();
}
