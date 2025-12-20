using System;
using System.ComponentModel.DataAnnotations;

namespace GymManagementSystem.Models.ViewModels
{
    public class MemberProfileViewModel
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon zorunludur.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Telefon numarası 10 haneli olmalıdır.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Boy alanı zorunludur.")]
        [Range(100, 250, ErrorMessage = "Boy 100 ile 250 cm arasında olmalıdır.")]
        public decimal? Height { get; set; }

        [Required(ErrorMessage = "Kilo alanı zorunludur.")]
        [Range(30, 200, ErrorMessage = "Kilo 30 ile 200 kg arasında olmalıdır.")]
        public decimal? Weight { get; set; }

        public DateTime RegisteredAt { get; set; }

        public string MembershipPlanName { get; set; } = "Plan Yok";

        public int TotalAppointments { get; set; }

        public int CompletedAppointments { get; set; }

        public string? ProfileImageUrl { get; set; }
    }
}
