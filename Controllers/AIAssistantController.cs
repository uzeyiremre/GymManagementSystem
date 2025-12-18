using GymManagementSystem.Data;
using GymManagementSystem.Models.Entities;
using GymManagementSystem.Models.ViewModels;
using GymManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Controllers
{
    [Authorize(Roles = "Member")]
    public class AIAssistantController : Controller
    {
        private readonly IOpenAIService _openAIService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AIAssistantController(IOpenAIService openAIService, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _openAIService = openAIService;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var history = await _context.AIConversations
                .Where(c => c.UserId == user.Id)
                .OrderBy(c => c.CreatedAt)
                .Take(100)
                .ToListAsync();

            var model = new AIAssistantViewModel
            {
                ConversationHistory = history
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] AIMessageRequest request)
        {
            if (request == null || (string.IsNullOrWhiteSpace(request.Message) && string.IsNullOrWhiteSpace(request.ImageBase64)))
            {
                return Json(new { success = false, error = "Mesaj boş olamaz." });
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, error = "Oturum bulunamadı." });
                }

                var userMessage = string.IsNullOrWhiteSpace(request.Message) ? "Lütfen bu fotoğrafı analiz et." : request.Message;
                await _openAIService.SaveConversationMessageAsync(user.Id, "user", userMessage);

                string aiResponse;
                if (!string.IsNullOrWhiteSpace(request.ImageBase64))
                {
                    aiResponse = await _openAIService.AnalyzeBodyPhotoAsync(request.ImageBase64, userMessage);
                }
                else
                {
                    aiResponse = await _openAIService.GetFitnessAdviceAsync(
                        userMessage,
                        request.Height,
                        request.Weight,
                        request.BodyType);
                }

                await _openAIService.SaveConversationMessageAsync(user.Id, "assistant", aiResponse);
                return Json(new { success = true, response = aiResponse });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var messages = _context.AIConversations.Where(c => c.UserId == user.Id);
            _context.AIConversations.RemoveRange(messages);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Sohbet geçmişi temizlendi!";
            return RedirectToAction(nameof(Index));
        }
    }
}
