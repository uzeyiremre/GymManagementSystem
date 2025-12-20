using System;
using GymManagementSystem.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GymManagementSystem.Models.Entities;
using GymManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        private const string AdminEmail = "b221210381@ogr.sakarya.edu.tr";
        private const string LegacyAdminEmail = "b221210381@sakarya.edu.tr";
        private const string AdminPassword = "sau";

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var input = model.Email?.Trim() ?? string.Empty;
                var user = await _userManager.FindByEmailAsync(input)
                    ?? await _userManager.FindByNameAsync(input);

                if (user == null && input.Equals(AdminEmail, StringComparison.OrdinalIgnoreCase))
                {
                    user = await _userManager.FindByEmailAsync(LegacyAdminEmail)
                        ?? await _userManager.FindByNameAsync(LegacyAdminEmail);
                }

                if (user != null)
                {
                    var userName = user.UserName ?? user.Email ?? user.Id;
                    var result = await _signInManager.PasswordSignInAsync(
                        userName,
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return LocalRedirect(returnUrl);
                        }

                        var roles = await _userManager.GetRolesAsync(user);

                        if (roles.Contains("Admin"))
                            return RedirectToAction("Dashboard", "Admin");
                        if (roles.Contains("Trainer"))
                            return RedirectToAction("Dashboard", "Trainer");
                        if (roles.Contains("Member"))
                            return RedirectToAction("Profile", "Member");

                        return RedirectToAction("AccessDenied", "Account");
                    }

                    if (result.IsLockedOut)
                    {
                        return RedirectToAction(nameof(Lockout));
                    }

                    ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Kullanıcı bulunamadı");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Member");

                    if (!await _context.Members.AnyAsync(m => m.UserId == user.Id))
                    {
                        var member = new Member
                        {
                            UserId = user.Id,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Email = user.Email ?? user.UserName ?? string.Empty,
                            Phone = model.PhoneNumber,
                            MembershipDate = DateTime.Now,
                            RegisteredAt = DateTime.Now,
                            IsActive = true
                        };

                        _context.Members.Add(member);
                        await _context.SaveChangesAsync();
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DebugAdmin()
        {
            var user = await _userManager.FindByEmailAsync(AdminEmail)
                ?? await _userManager.FindByNameAsync(AdminEmail)
                ?? await _userManager.FindByEmailAsync(LegacyAdminEmail)
                ?? await _userManager.FindByNameAsync(LegacyAdminEmail)
                ?? await _userManager.Users.FirstOrDefaultAsync(u =>
                    u.NormalizedEmail == AdminEmail.ToUpperInvariant() ||
                    u.NormalizedEmail == LegacyAdminEmail.ToUpperInvariant());

            if (user == null)
            {
                return Ok("Kullanıcı bulunamadı");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var canLogin = await _signInManager.CheckPasswordSignInAsync(user, AdminPassword, false);

            return Ok(new
            {
                UserId = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                Roles = roles,
                CanLogin = canLogin.Succeeded
            });
        }
    }
}
