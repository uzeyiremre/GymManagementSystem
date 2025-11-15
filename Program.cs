using GymManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using GymManagementSystem.Data;
using GymManagementSystem.Models.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 3;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddErrorDescriber<TurkishIdentityErrorDescriber>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedAdminUser(services);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static async Task SeedAdminUser(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roleNames = { "Admin", "Member", "Trainer" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    const string adminEmail = "b221210381@ogr.sakarya.edu.tr";
    const string legacyAdminEmail = "b221210381@sakarya.edu.tr";
    const string adminPassword = "sau";

    var normalizedEmail = adminEmail.ToUpperInvariant();
    var legacyNormalizedEmail = legacyAdminEmail.ToUpperInvariant();

    var adminUser = await userManager.FindByEmailAsync(adminEmail)
        ?? await userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail)
        ?? await userManager.FindByEmailAsync(legacyAdminEmail)
        ?? await userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == legacyNormalizedEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Gozeyir Emre",
            LastName = "Turkmen",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (!createResult.Succeeded)
        {
            Console.WriteLine("[AdminSeed] Admin kullanıcısı oluşturulamadı:");
            foreach (var error in createResult.Errors)
            {
                Console.WriteLine($"   - {error.Description}");
            }
            return;
        }

        await userManager.AddToRoleAsync(adminUser, "Admin");
        Console.WriteLine($"[AdminSeed] Admin kullanıcısı oluşturuldu: {adminEmail}");
        return;
    }

    var requiresUpdate = false;

    if (!string.Equals(adminUser.Email, adminEmail, StringComparison.OrdinalIgnoreCase))
    {
        adminUser.Email = adminEmail;
        requiresUpdate = true;
    }

    if (!string.Equals(adminUser.UserName, adminEmail, StringComparison.OrdinalIgnoreCase))
    {
        adminUser.UserName = adminEmail;
        requiresUpdate = true;
    }

    if (!adminUser.EmailConfirmed)
    {
        adminUser.EmailConfirmed = true;
        requiresUpdate = true;
    }

    if (requiresUpdate)
    {
        var updateResult = await userManager.UpdateAsync(adminUser);
        if (!updateResult.Succeeded)
        {
            Console.WriteLine("[AdminSeed] Admin kullanıcı güncellenemedi:");
            foreach (var error in updateResult.Errors)
            {
                Console.WriteLine($"   - {error.Description}");
            }
        }
    }

    await userManager.UpdateNormalizedEmailAsync(adminUser);
    await userManager.UpdateNormalizedUserNameAsync(adminUser);

    var resetToken = await userManager.GeneratePasswordResetTokenAsync(adminUser);
    var resetResult = await userManager.ResetPasswordAsync(adminUser, resetToken, adminPassword);

    if (resetResult.Succeeded)
    {
        Console.WriteLine("[AdminSeed] Admin şifresi güncellendi.");
    }
    else
    {
        Console.WriteLine("[AdminSeed] Admin şifresi güncellenemedi:");
        foreach (var error in resetResult.Errors)
        {
            Console.WriteLine($"   - {error.Description}");
        }
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
        Console.WriteLine("[AdminSeed] Admin rolü eklendi.");
    }
}
