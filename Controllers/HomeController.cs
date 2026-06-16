using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.ViewModels;

namespace UserManagement.Controllers;

// Restricts the entire controller to authenticated sessions only
[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    // Injecting the database context session session provider
    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// Renders the core administrative User Management panel dashboard.
    
    public async Task<IActionResult> Index()
    {
        // Retrieve data ordered chronologically by the last activity/login field
        // This ensures the newest login events consistently bubble to the top of the grid view
        var usersList = await _context.Users
            .OrderByDescending(u => u.LastLogin)
            .ToListAsync();

        // Bind the structured data collection into the expected UI data schema pipeline
        var viewModel = new UserManagementViewModel
        {
            Users = usersList
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}