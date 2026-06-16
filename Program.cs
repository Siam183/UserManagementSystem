using Microsoft.AspNetCore.Authentication.Cookies;
using UserManagement.Models;
using UserManagement.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UserManagement.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(Options =>
    {
        Options.LoginPath = "/Account/Login";
        Options.AccessDeniedPath = "/Account/Login";
    });

// Add services to the container.
builder.Services.AddControllersWithViews();
var emailSettings =
    builder.Configuration
        .GetSection("EmailSettings")
        .Get<EmailSettings>();

builder.Services.AddSingleton(
    new EmailService(emailSettings!));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseMiddleware<UserManagement.Middleware.UserStatusMiddleware>();

app.UseAuthorization();

app.UseStaticFiles();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
