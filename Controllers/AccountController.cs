using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.ViewModels;
using UserManagement.Services;

namespace UserManagement.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly EmailService _emailService;

    public AccountController(
        ApplicationDbContext context,
        EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
        _passwordHasher = new PasswordHasher<User>();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new User
        {
            Name = model.Name,
            Email = model.Email,
            RegistrationDate = DateTime.Now,
            Status = UserStatus.Unverified,
            VerificationToken = Guid.NewGuid().ToString()
        };

        // Securely hash the password before saving
        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        try
        {
            // No pre-check code queries
            // letting the Database Unique Index constraint handle duplicates.
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Send asynchronous email token validation loop
            var verificationLink = Url.Action(
                "VerifyEmail",
                "Account",
                new { token = user.VerificationToken },
                Request.Scheme);

            await _emailService.SendVerificationEmail(user.Email, verificationLink!);

            TempData["Success"] = "Registration successful. Check your email for verification.";
        }
        catch (DbUpdateException)
        {
            // Catching the database structural uniqueness violation error and printing a clear form validation rule
            ModelState.AddModelError("", "Email already exists.");
            return View(model);
        }

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);

        if (user == null)
        {
            TempData["Error"] = "Invalid verification link.";
            return RedirectToAction(nameof(Login));
        }

        // State Machine transition rules
        if (user.Status == UserStatus.Unverified)
        {
            user.Status = UserStatus.Active;
            user.VerificationToken = null; // Clear token after success

            await _context.SaveChangesAsync();
            TempData["Success"] = "Email verified successfully.";
        }
        else if (user.Status == UserStatus.Blocked)
        {
            TempData["Error"] = "Blocked users cannot be verified.";
        }
        else
        {
            TempData["Success"] = "Account already verified.";
        }

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? message)
    {
        // Intercept middleware redirects to display user-friendly contextual alert errors
        if (message == "blocked")
        {
            ViewBag.ErrorMessage = "Your account has been blocked.";
        }
        if (message == "deleted")
        {
            ViewBag.ErrorMessage = "Your account no longer exists.";
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);

        if (user == null)
        {
            ModelState.AddModelError("", "Invalid email.");
            return View(model);
        }

        // Prevent blocked profiles from initiating active identity context
        if (user.Status == UserStatus.Blocked)
        {
            ModelState.AddModelError("", "Your account has been blocked.");
            return View(model);
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "Invalid password.");
            return View(model);
        }

        // Generate the authentication session claims identity layout
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        
        // Persist session context via browser authorization cookies
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        
        //Track and log chronological login values for view sorting
        user.LastLogin = DateTime.Now;
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Logout()
    {
        // Expire cookie authentication references explicitly
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ResendVerification()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResendVerification(ResendVerificationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null)
        {
            ModelState.AddModelError("", "User not found.");
            return View(model);
        }

        if (user.Status == UserStatus.Active)
        {
            ModelState.AddModelError("", "Account is already verified.");
            return View(model);
        }

        // Refresh validation token state completely
        user.VerificationToken = Guid.NewGuid().ToString();
        await _context.SaveChangesAsync();

        var verificationLink = Url.Action(
            "VerifyEmail",
            "Account",
            new { token = user.VerificationToken },
            Request.Scheme);

        await _emailService.SendVerificationEmail(user.Email, verificationLink!);

        TempData["Success"] = "Verification email sent.";
        return RedirectToAction(nameof(Login));
    }
}