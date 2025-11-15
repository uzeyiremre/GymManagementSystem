using Microsoft.AspNetCore.Identity;

namespace GymManagementSystem.Models.Entities
{
    // Kullanıcı bilgileri
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        // İlişkiler
        public Member? Member { get; set; }
        public Trainer? Trainer { get; set; }
    }
}
