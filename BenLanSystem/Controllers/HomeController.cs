using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BenLanSystem.Models;
using BenLanSystem.Services.Interfaces;

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

    public async Task<IActionResult> Blog([FromServices] IBlogService blogService)
    {
        ViewData["ActivePage"] = "Blog";
        var posts = await blogService.GetPublishedAsync(1, 20);
        return View(posts);
    }

    public async Task<IActionResult> BlogDetail(long id, [FromServices] IBlogService blogService)
    {
        var post = await blogService.GetByIdAsync(id);
        if (post is null) return RedirectToAction("Blog");
        ViewData["ActivePage"] = "Blog";
        return View(post);
    }

    [Authorize]
    public IActionResult MyBookings()
    {
        ViewData["Title"] = "My Bookings";
        ViewData["ActivePage"] = "MyBookings";
        return View();
    }

    [Authorize]
    public IActionResult Pay(long bookingId)
    {
        ViewData["BookingId"] = bookingId;
        ViewData["Title"] = "Payment";
        ViewData["ActivePage"] = "Book";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}