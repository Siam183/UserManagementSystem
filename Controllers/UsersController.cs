using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.ViewModels;

namespace UserManagement.Controllers;

// Restricts the entire management panel to authenticated sessions only
[Authorize]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;

    public UsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// Displays the administrative user grid view data payload.

    public async Task<IActionResult> Index()
    {
        var model = new UserManagementViewModel
        {
            // Explicit primary sort configuration using descending chronological order
            Users = await _context.Users
                .OrderByDescending(u => u.LastLogin)
                .ThenByDescending(u => u.RegistrationDate)
                .ToListAsync()
        };

        return View(model);
    }


    /// Processes bulk status blocking updates.

    [HttpPost]
    public async Task<IActionResult> Block(UserManagementViewModel model)
    {
        // Maps array inputs generated via form checkbox selections
        var users = await _context.Users
            .Where(u => model.SelectedUserIds.Contains(u.Id))
            .ToListAsync();

        foreach (var user in users)
        {
            user.Status = UserStatus.Blocked;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"{users.Count} user(s) blocked.";
        return RedirectToAction(nameof(Index));
    }

    /// Restores user operational permissions conditionally.

    [HttpPost]
    public async Task<IActionResult> Unblock(UserManagementViewModel model)
    {
        var users = await _context.Users
            .Where(u => model.SelectedUserIds.Contains(u.Id))
            .ToListAsync();

        foreach (var user in users)
        {
            if (user.Status == UserStatus.Blocked)
            {
                // Reverts users safely based on past email validation tokens
                if (string.IsNullOrEmpty(user.VerificationToken))
                {
                    user.Status = UserStatus.Active;
                }
                else
                {
                    user.Status = UserStatus.Unverified;
                }
            }
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Selected users unblocked.";
        return RedirectToAction(nameof(Index));
    }

    /// Performs hard permanent deletions.

    [HttpPost]
    public async Task<IActionResult> Delete(UserManagementViewModel model)
    {
        var users = await _context.Users
            .Where(u => model.SelectedUserIds.Contains(u.Id))
            .ToListAsync();

        // Explicit physical row deletion 
        _context.Users.RemoveRange(users);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"{users.Count} user(s) deleted.";
        return RedirectToAction(nameof(Index));
    }

    /// Discards all unverified accounts at a cluster level.

    [HttpPost]
    public async Task<IActionResult> DeleteUnverified()
    {
        // Scopes out all non-validated registration footprints globally
        var users = await _context.Users
            .Where(u => u.Status == UserStatus.Unverified)
            .ToListAsync();

        _context.Users.RemoveRange(users);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"{users.Count} unverified user(s) deleted.";
        return RedirectToAction(nameof(Index));
    }
}