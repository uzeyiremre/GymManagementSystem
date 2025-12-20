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
    public class MemberController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MemberController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = await _context.Members
                .Include(m => m.User)
                .Include(m => m.MembershipPlan)
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (member == null)
            {
                TempData["ErrorMessage"] = "Üye kaydınız bulunamadı.";
                return RedirectToAction("Index", "Home");
            }

            var model = new MemberProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? member.Phone,
                Height = member.Height,
                Weight = member.Weight,
                RegisteredAt = member.RegisteredAt,
                MembershipPlanName = member.MembershipPlan?.Name ?? "Plan Yok",
                TotalAppointments = await _context.Appointments.CountAsync(a => a.MemberId == member.MemberId),
                CompletedAppointments = await _context.Appointments.CountAsync(a => a.MemberId == member.MemberId && a.Status == "Completed"),
                ProfileImageUrl = member.ProfileImageUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(MemberProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Lütfen zorunlu alanları kontrol edin.";
                return View("Profile", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == user.Id);
            if (member == null)
            {
                TempData["ErrorMessage"] = "Üye kaydı bulunamadı.";
                return RedirectToAction(nameof(Profile));
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            member.Phone = model.PhoneNumber;
            member.Height = model.Height;
            member.Weight = model.Weight;

            await _userManager.UpdateAsync(user);
            _context.Members.Update(member);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profil güncellendi!";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["ErrorMessage"] = "Şifre alanları boş olamaz.";
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Şifre değiştirildi!";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            }
            return RedirectToAction(nameof(Profile));
        }
    }
}
