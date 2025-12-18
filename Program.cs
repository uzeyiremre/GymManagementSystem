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

builder.Services.AddHttpClient();
builder.Services.AddScoped<IOpenAIService, GeminiService>();

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
    var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

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
    }
    else
    {
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

    var defaultGymId = await dbContext.Gyms.Select(g => g.GymId).FirstOrDefaultAsync();
    if (defaultGymId == 0)
    {
        var gym = new Gym
        {
            Name = "Merkez Salon",
            Address = "Sakarya",
            Phone = "+90 555 000 00 00",
            OpeningTime = new TimeSpan(6, 0, 0),
            ClosingTime = new TimeSpan(23, 0, 0),
            Description = "Varsayılan spor salonu"
        };
        dbContext.Gyms.Add(gym);
        await dbContext.SaveChangesAsync();
        defaultGymId = gym.GymId;
    }

    async Task EnsureTrainerAsync(string email, string firstName, string lastName, string specialization, int experienceYears)
    {
        var trainerUser = await userManager.FindByEmailAsync(email);
        if (trainerUser == null)
        {
            trainerUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true
            };
            var createResult = await userManager.CreateAsync(trainerUser, "Trainer123");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(trainerUser, "Trainer");
            }
        }

        var trainerRecord = await dbContext.Trainers.FirstOrDefaultAsync(t => t.UserId == trainerUser.Id);
        if (trainerRecord == null)
        {
            dbContext.Trainers.Add(new Trainer
            {
                UserId = trainerUser.Id,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Specialization = specialization,
                ExperienceYears = experienceYears,
                HourlyRate = 150m,
                GymId = defaultGymId,
                IsActive = true
            });
        }
        else
        {
            trainerRecord.Specialization = specialization;
            trainerRecord.ExperienceYears = experienceYears;
            trainerRecord.IsActive = true;
        }
    }

    await EnsureTrainerAsync("ahmet@gym.com", "Ahmet", "Yılmaz", "Vücut Geliştirme", 5);
    await EnsureTrainerAsync("zeynep@gym.com", "Zeynep", "Kaya", "Yoga & Pilates", 3);
    await dbContext.SaveChangesAsync();
}
