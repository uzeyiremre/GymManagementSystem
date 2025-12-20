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
    [Authorize(Roles = "Admin,Trainer")]
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

            var today = DateTime.Today;

            // If admin has no trainer record, show aggregated overview instead of blocking access.
            var appointmentsQuery = _context.Appointments
                .Include(a => a.Member)!.ThenInclude(m => m!.User)
                .Include(a => a.Service)
                .AsQueryable();

            if (trainer != null)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.TrainerId == trainer.TrainerId);
            }

            var upcomingQuery = appointmentsQuery
                .Where(a => a.AppointmentDate > DateTime.Now)
                .OrderBy(a => a.AppointmentDate);

            var viewModel = new TrainerDashboardViewModel
            {
                TodayAppointmentsCount = await appointmentsQuery.CountAsync(a => a.AppointmentDate.Date == today),
                TodayAppointments = await appointmentsQuery
                    .Where(a => a.AppointmentDate.Date == today)
                    .OrderBy(a => a.AppointmentDate)
                    .ToListAsync(),
                UpcomingAppointmentsCount = await upcomingQuery.CountAsync(),
                UpcomingAppointments = await upcomingQuery.Take(5).ToListAsync(),
                PendingRequestsCount = await appointmentsQuery.CountAsync(a => a.Status == "Pending"),
                MonthlyRevenue = await appointmentsQuery
                    .Where(a => a.Status == "Completed"
                        && a.AppointmentDate.Month == DateTime.Now.Month
                        && a.AppointmentDate.Year == DateTime.Now.Year)
                    .SumAsync(a => a.TotalPrice),
                TotalClients = await appointmentsQuery
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

            var query = _context.Appointments
                .Include(a => a.Member)!.ThenInclude(m => m!.User)
                .Include(a => a.Service)
                .AsQueryable();

            // If trainer exists, scope to that trainer; otherwise (admin without trainer) show all.
            if (trainer != null)
            {
                query = query.Where(a => a.TrainerId == trainer.TrainerId);
            }

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

            // If trainer exists, limit to that trainer; if admin without trainer, allow any appointment by id.
            var appointmentQuery = _context.Appointments.AsQueryable();
            if (trainer != null)
            {
                appointmentQuery = appointmentQuery.Where(a => a.TrainerId == trainer.TrainerId);
            }

            var appointment = await appointmentQuery.FirstOrDefaultAsync(a => a.AppointmentId == id);

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
