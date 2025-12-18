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

        public string? PhoneNumber { get; set; }

        public DateTime RegisteredAt { get; set; }

        public string MembershipPlanName { get; set; } = "Plan Yok";

        public int TotalAppointments { get; set; }

        public int CompletedAppointments { get; set; }

        public string? ProfileImageUrl { get; set; }
    }
}
