using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymManagementSystem.Data;
using GymManagementSystem.Models.Entities;
using GymManagementSystem.Models.ViewModels;

namespace GymManagementSystem.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class TrainerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TrainerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var trainer = await _context.Trainers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (trainer == null)
            {
                TempData["ErrorMessage"] = "Antrenör kaydı bulunamadı.";
                return RedirectToAction("Index", "Home");
            }

            var today = DateTime.Today;
            var upcomingQuery = _context.Appointments
                .Where(a => a.TrainerId == trainer.TrainerId && a.AppointmentDate > DateTime.Now)
                .Include(a => a.Member)!.ThenInclude(m => m!.User)
                .Include(a => a.Service)
                .OrderBy(a => a.AppointmentDate);

            var viewModel = new TrainerDashboardViewModel
            {
                TodayAppointmentsCount = await _context.Appointments.CountAsync(a =>
                    a.TrainerId == trainer.TrainerId && a.AppointmentDate.Date == today),
                TodayAppointments = await _context.Appointments
                    .Where(a => a.TrainerId == trainer.TrainerId && a.AppointmentDate.Date == today)
                    .Include(a => a.Member)!.ThenInclude(m => m!.User)
                    .Include(a => a.Service)
                    .OrderBy(a => a.AppointmentDate)
                    .ToListAsync(),
                UpcomingAppointmentsCount = await upcomingQuery.CountAsync(),
                UpcomingAppointments = await upcomingQuery.Take(5).ToListAsync(),
                MonthlyRevenue = await _context.Appointments
                    .Where(a => a.TrainerId == trainer.TrainerId
                        && a.Status == "Completed"
                        && a.AppointmentDate.Month == DateTime.Now.Month
                        && a.AppointmentDate.Year == DateTime.Now.Year)
                    .SumAsync(a => a.TotalPrice),
                TotalClients = await _context.Appointments
                    .Where(a => a.TrainerId == trainer.TrainerId)
                    .Select(a => a.MemberId)
                    .Distinct()
                    .CountAsync()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> MyAppointments(string? status = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (trainer == null)
            {
                TempData["ErrorMessage"] = "Antrenör kaydı bulunamadı.";
                return RedirectToAction("Index", "Home");
            }

            var query = _context.Appointments
                .Where(a => a.TrainerId == trainer.TrainerId)
                .Include(a => a.Member)!.ThenInclude(m => m!.User)
                .Include(a => a.Service)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.Status == status);
            }

            var appointments = await query
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            ViewBag.CurrentStatus = status ?? "All";
            return View(appointments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(int id, string? notes)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (trainer == null)
            {
                TempData["ErrorMessage"] = "Antrenör kaydı bulunamadı.";
                return RedirectToAction(nameof(MyAppointments));
            }

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == id && a.TrainerId == trainer.TrainerId);

            if (appointment != null)
            {
                appointment.Status = "Completed";
                appointment.Notes = notes;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Randevu tamamlandı!";
            }
            else
            {
                TempData["ErrorMessage"] = "Randevu bulunamadı.";
            }

            return RedirectToAction(nameof(MyAppointments));
        }

        public IActionResult Availability()
        {
            return View();
        }
    }
}
