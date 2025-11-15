using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GymManagementSystem.Models.Entities;
using GymManagementSystem.Models.ViewModels;


namespace GymManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // Giriş sayfası
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // Giriş işlemi
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // Kullanıcı giriş dene
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    return LocalRedirect(returnUrl ?? "/Home/Index");
                }

                if (result.IsLockedOut)
                {
                    return RedirectToAction(nameof(Lockout));
                }

                ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi");
            }

            return View(model);
        }

        // Kayıt sayfası
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Kayıt işlemi
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Yeni kullanıcı oluştur
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailConfirmed = true
                };

                // Şifre ile kaydet
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Member rolü ekle
                    await _userManager.AddToRoleAsync(user, "Member");

                    // Otomatik giriş yap
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return RedirectToAction("Index", "Home");
                }

                // Hataları göster
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // Çıkış
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // Kilit açma
        public IActionResult Lockout()
        {
            return View();
        }
    }
}
