using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BenLanSystem.Models;

namespace BenLanSystem.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
 
    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult BookTicket()
    {
        ViewData["Title"] = "Book Ticket";
        ViewData["ActivePage"] = "Book";
        return View();
    }

    public IActionResult Blog()
    {
        ViewData["ActivePage"] = "Blog";
        return View();
    }

    public IActionResult BlogDetail(int id = 1)
    {
        if (id < 1 || id > 2) return RedirectToAction("Blog");
        ViewData["ActivePage"] = "Blog";
        ViewData["BlogId"] = id;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}