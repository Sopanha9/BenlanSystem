using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenLanSystem.Controllers.Admin;

[Authorize(Roles = "Admin,Staff")]
[Area("Admin")]
public class RoutesController : Controller
{
    public IActionResult Index() => View();
}
