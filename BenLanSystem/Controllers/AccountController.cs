using BenLanSystem.Models;
using BenLanSystem.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BenLanSystem.Controllers;

public class AccountController(UserManager<Staff> userManager, SignInManager<Staff> signInManager, RoleManager<IdentityRole<long>> roleManager) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ActivePage"] = "Login";
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ActivePage"] = "Login";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        ViewData["ActivePage"] = "Register";
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        ViewData["ActivePage"] = "Register";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var nameParts = model.FullName?.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var user = new Staff
        {
            UserName = model.Email,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            FirstName = nameParts is { Length: > 0 } ? nameParts[0] : "",
            LastName = nameParts is { Length: > 1 } ? nameParts[1] : "",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Ensure Customer role exists
            if (!await roleManager.RoleExistsAsync("Customer"))
            {
                await roleManager.CreateAsync(new IdentityRole<long>("Customer"));
            }
            await userManager.AddToRoleAsync(user, "Customer");

            await signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}