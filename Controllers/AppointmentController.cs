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
    [Authorize(Roles = "Member")]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var appointments = await _context.Appointments
                .Where(a => a.MemberId == member.MemberId)
                .Include(a => a.Trainer)
                    .ThenInclude(t => t!.User)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                return RedirectToAction("Login", "Account");
            }

            await PopulateTrainersAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAppointmentViewModel model)
        {
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                await PopulateTrainersAsync();
                return View(model);
            }

            var hasConflict = await _context.Appointments.AnyAsync(a =>
                a.TrainerId == model.TrainerId &&
                a.AppointmentDate == model.AppointmentDate &&
                a.Status == "Scheduled");

            if (hasConflict)
            {
                ModelState.AddModelError(string.Empty, "Bu antrenörün seçilen tarihte randevusu var.");
                await PopulateTrainersAsync();
                return View(model);
            }

            var appointment = new Appointment
            {
                MemberId = member.MemberId,
                TrainerId = model.TrainerId,
                AppointmentDate = model.AppointmentDate,
                Notes = model.Notes,
                Status = "Scheduled"
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Randevu başarıyla oluşturuldu!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var member = await GetCurrentMemberAsync();
            if (member == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == id && a.MemberId == member.MemberId);

            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Randevu iptal edildi.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<Member?> GetCurrentMemberAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return null;
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == user.Id);
            if (member != null)
            {
                return member;
            }

            member = new Member
            {
                UserId = user.Id,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? user.UserName ?? string.Empty,
                MembershipDate = DateTime.Now,
                RegisteredAt = DateTime.Now,
                IsActive = true
            };

            _context.Members.Add(member);
            await _context.SaveChangesAsync();

            return member;
        }

        private async Task PopulateTrainersAsync()
        {
            var trainers = await _context.Trainers
                .Include(t => t.User)
                .Where(t => t.IsActive)
                .OrderBy(t => t.User != null ? t.User.FirstName : t.FirstName)
                .ToListAsync();

            ViewBag.Trainers = trainers;
        }
    }
}
