using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using UserManagement.Data;
using UserManagement.Models;

namespace UserManagement.Middleware;

public class UserStatusMiddleware
{
    private readonly RequestDelegate _next;

    public UserStatusMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// Evaluates the active database state of an authenticated user on every incoming HTTP request pipeline iteration.

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        // Only evaluate security state if the user has an active identity cookie context
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null)
            {
                // Fallback verification: Sign out immediately if the claim id cannot be processed safely
                if (!int.TryParse(userIdClaim.Value, out int userId))
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/Account/Login", false);
                    return;
                }

                // Query the database directly to fetch the real-time record state
                var user = await db.Users.FindAsync(userId);

                // If an admin permanently removes this user record, force an immediate session drop
                if (user == null)
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/Account/Login?message=deleted", false);
                    return;
                }

                // If an admin blocks this user, intercept their next request click and revoke access instantly
                if (user.Status == UserStatus.Blocked)
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/Account/Login?message=blocked", false);
                    return;
                }
            }
        }

        // Proceed down the standard execution chain if user identity remains validated
        await _next(context);
    }
}