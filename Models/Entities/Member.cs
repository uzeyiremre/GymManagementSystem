using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementSystem.Models.Entities
{
    // Üye bilgileri
    public class Member
    {
        [Key]
        public int MemberId { get; set; }

        [NotMapped]
        public int Id
        {
            get => MemberId;
            set => MemberId = value;
        }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Height { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Weight { get; set; }

        public string? BodyType { get; set; }

        public string? FitnessGoal { get; set; }

        public string? ProfileImageUrl { get; set; }

        public DateTime MembershipDate { get; set; } = DateTime.Now;

        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public int? MembershipPlanId { get; set; }

        // Kullanıcı bağlantısı
        public string? UserId { get; set; }

        // İlişkiler
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [ForeignKey("MembershipPlanId")]
        public MembershipPlan? MembershipPlan { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
