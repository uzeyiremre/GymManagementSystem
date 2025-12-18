using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymManagementSystem.Data;
using GymManagementSystem.Models.Entities;
using GymManagementSystem.Models.ViewModels;

namespace GymManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var now = DateTime.Now;

            var viewModel = new AdminDashboardViewModel
            {
                TotalMembers = await _context.Members.CountAsync(),
                TotalTrainers = await _context.Trainers.CountAsync(),
                PendingAppointments = await _context.Appointments.CountAsync(a => a.Status == "Pending" || a.Status == "Scheduled"),
                TodayAppointments = await _context.Appointments.CountAsync(a => a.AppointmentDate.Date == today),
                MonthlyRevenue = await _context.Appointments
                    .Where(a => a.Status == "Completed" && a.AppointmentDate.Month == now.Month && a.AppointmentDate.Year == now.Year)
                    .SumAsync(a => a.TotalPrice),
                RecentMembers = await _context.Members
                    .Include(m => m.User)
                    .OrderByDescending(m => m.RegisteredAt)
                    .Take(5)
                    .ToListAsync(),
                RecentAppointments = await _context.Appointments
                    .Include(a => a.Member)!.ThenInclude(m => m!.User)
                    .Include(a => a.Trainer)!.ThenInclude(t => t!.User)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Members(string? searchTerm)
        {
            var query = _context.Members
                .Include(m => m.User)
                .Include(m => m.MembershipPlan)
                .OrderByDescending(m => m.RegisteredAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var pattern = $"%{searchTerm.Trim()}%";
                query = query.Where(m =>
                    EF.Functions.Like(m.FirstName + " " + m.LastName, pattern) ||
                    (m.User != null && EF.Functions.Like(m.User.FirstName + " " + m.User.LastName, pattern)) ||
                    EF.Functions.Like(m.Email, pattern));
            }

            var viewModel = new MemberListViewModel
            {
                Members = await query.ToListAsync(),
                SearchTerm = searchTerm
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Trainers()
        {
            var trainers = await _context.Trainers
                .Include(t => t.User)
                .OrderBy(t => t.User != null ? t.User.FirstName : t.FirstName)
                .ToListAsync();

            return View(trainers);
        }

        [HttpGet]
        public IActionResult CreateTrainer()
        {
            return View(new TrainerFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrainer(TrainerFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var gymId = await _context.Gyms.Select(g => g.GymId).FirstOrDefaultAsync();
            if (gymId == 0)
            {
                ModelState.AddModelError(string.Empty, "Önce en az bir spor salonu oluşturulmalıdır.");
                return View(model);
            }

            var trainer = new Trainer
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Phone = model.Phone,
                Specialization = model.Specialization,
                Bio = model.Bio,
                HourlyRate = model.HourlyRate,
                IsActive = model.IsActive,
                ExperienceYears = model.ExperienceYears,
                GymId = gymId
            };

            _context.Trainers.Add(trainer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Antrenör başarıyla eklendi.";
            return RedirectToAction(nameof(Trainers));
        }

        [HttpGet]
        public async Task<IActionResult> EditTrainer(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer == null)
            {
                return NotFound();
            }

            var viewModel = new TrainerFormViewModel
            {
                TrainerId = trainer.TrainerId,
                FirstName = trainer.FirstName,
                LastName = trainer.LastName,
                Email = trainer.Email,
                Phone = trainer.Phone,
                Specialization = trainer.Specialization,
                Bio = trainer.Bio,
                HourlyRate = trainer.HourlyRate,
                ExperienceYears = trainer.ExperienceYears,
                IsActive = trainer.IsActive
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTrainer(TrainerFormViewModel model)
        {
            if (!ModelState.IsValid || model.TrainerId == null)
            {
                return View(model);
            }

            var trainer = await _context.Trainers.FindAsync(model.TrainerId.Value);
            if (trainer == null)
            {
                return NotFound();
            }

            trainer.FirstName = model.FirstName;
            trainer.LastName = model.LastName;
            trainer.Email = model.Email;
            trainer.Phone = model.Phone;
            trainer.Specialization = model.Specialization;
            trainer.Bio = model.Bio;
            trainer.HourlyRate = model.HourlyRate;
            trainer.ExperienceYears = model.ExperienceYears;
            trainer.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Antrenör bilgileri güncellendi.";
            return RedirectToAction(nameof(Trainers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrainer(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer != null)
            {
                _context.Trainers.Remove(trainer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Antrenör silindi.";
            }

            return RedirectToAction(nameof(Trainers));
        }

        public async Task<IActionResult> Appointments(string? status, string? searchTerm, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Appointments
                .Include(a => a.Member)!.ThenInclude(m => m!.User)
                .Include(a => a.Trainer)!.ThenInclude(t => t!.User)
                .Include(a => a.Service)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.Status == status);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.AppointmentDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.AppointmentDate <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var pattern = $"%{searchTerm.Trim()}%";
                query = query.Where(a =>
                    (a.Member != null && (
                        EF.Functions.Like(a.Member.FirstName + " " + a.Member.LastName, pattern) ||
                        (a.Member.User != null && EF.Functions.Like(a.Member.User.FirstName + " " + a.Member.User.LastName, pattern)) ||
                        EF.Functions.Like(a.Member.Email, pattern))) ||
                    (a.Trainer != null && (
                        EF.Functions.Like(a.Trainer.FirstName + " " + a.Trainer.LastName, pattern) ||
                        (a.Trainer.User != null && EF.Functions.Like(a.Trainer.User.FirstName + " " + a.Trainer.User.LastName, pattern)))));
            }

            var appointments = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var viewModel = new AppointmentListViewModel
            {
                Appointments = appointments,
                Status = status,
                SearchTerm = searchTerm,
                StartDate = startDate,
                EndDate = endDate
            };

            return View(viewModel);
        }

        public async Task<IActionResult> MemberDetails(int id)
        {
            var member = await _context.Members
                .Include(m => m.User)
                .Include(m => m.MembershipPlan)
                .Include(m => m.Appointments)!
                    .ThenInclude(a => a.Trainer!)
                        .ThenInclude(t => t!.User)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        public async Task<IActionResult> TrainerDetails(int id)
        {
            var trainer = await _context.Trainers
                .Include(t => t.User)
                .Include(t => t.Appointments)!
                    .ThenInclude(a => a.Member!)
                        .ThenInclude(m => m!.User)
                .FirstOrDefaultAsync(t => t.TrainerId == id);

            if (trainer == null)
            {
                return NotFound();
            }

            return View(trainer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Randevu iptal edildi.";
            return RedirectToAction(nameof(Appointments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = "Scheduled";
                appointment.ConfirmedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Randevu onaylandı!";
            }

            return RedirectToAction(nameof(Appointments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Randevu reddedildi.";
            }

            return RedirectToAction(nameof(Appointments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = "Completed";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Randevu tamamlandı!";
            }

            return RedirectToAction(nameof(Appointments));
        }

        public async Task<IActionResult> Reports()
        {
            var startDate = DateTime.Now.AddMonths(-6);

            var monthlyRevenue = await _context.Appointments
                .Where(a => a.Status == "Completed" && a.AppointmentDate >= startDate)
                .GroupBy(a => new { a.AppointmentDate.Year, a.AppointmentDate.Month })
                .Select(g => new MonthlyRevenuePoint(g.Key.Year, g.Key.Month, g.Sum(a => a.TotalPrice)))
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            var topTrainers = await _context.Appointments
                .Where(a => a.Status == "Completed" && a.Trainer != null)
                .GroupBy(a => new
                {
                    a.TrainerId,
                    Name = a.Trainer!.User != null
                        ? a.Trainer.User.FirstName + " " + a.Trainer.User.LastName
                        : a.Trainer.FirstName + " " + a.Trainer.LastName
                })
                .Select(g => new TopTrainerStat(g.Key.Name, g.Count(), g.Sum(a => a.TotalPrice)))
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync();

            var memberStats = new MemberReportStats(
                await _context.Members.CountAsync(),
                await _context.Members.CountAsync(m => m.RegisteredAt >= DateTime.Now.AddMonths(-1)),
                await _context.Members.CountAsync(m => m.RegisteredAt.Month == DateTime.Now.Month && m.RegisteredAt.Year == DateTime.Now.Year));

            var statusDistribution = await _context.Appointments
                .GroupBy(a => a.Status)
                .Select(g => new StatusDistributionPoint(g.Key, g.Count()))
                .ToListAsync();

            var viewModel = new AdminReportsViewModel
            {
                MonthlyRevenue = monthlyRevenue,
                TopTrainers = topTrainers,
                MemberStats = memberStats,
                StatusDistribution = statusDistribution
            };

            return View(viewModel);
        }
    }
}

