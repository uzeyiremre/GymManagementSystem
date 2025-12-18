using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GymManagementSystem.Models;
using GymManagementSystem.Models.ViewModels;
using GymManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new HomeLandingViewModel
        {
            TotalMembers = await _context.Members.CountAsync(),
            ActiveTrainers = await _context.Trainers.CountAsync(t => t.IsActive),
            CompletedAppointments = await _context.Appointments.CountAsync(a => a.Status == "Completed"),
            FeaturedTrainers = await _context.Trainers
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.ExperienceYears)
                .Take(3)
                .Include(t => t.User)
                .ToListAsync()
        };

        return View(model);
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
