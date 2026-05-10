using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BenLanSystem.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class StaffController : Controller
{
    public IActionResult Index() => View();
}
