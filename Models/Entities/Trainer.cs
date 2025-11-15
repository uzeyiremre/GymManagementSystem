using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace GymManagementSystem.Models.Entities
{
    // Antrenör bilgileri
    public class Trainer
    {
        [Key]
        public int TrainerId { get; set; }

        [NotMapped]
        public int Id
        {
            get => TrainerId;
            set => TrainerId = value;
        }

        [Required(ErrorMessage = "Antrenör adı zorunludur")]
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

        [Required]
        [StringLength(50)]
        public string Specialization { get; set; } = string.Empty;

        public string? Bio { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public string? UserId { get; set; }

        // Bağlı salon
        public int GymId { get; set; }

        // İlişkiler
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [ForeignKey("GymId")]
        public Gym? Gym { get; set; }
        public ICollection<TrainerAvailability>? Availabilities { get; set; }
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<Service>? Services { get; set; }

        [NotMapped]
        public bool IsAvailable => Availabilities?.Any(a => a.IsAvailable) ?? false;
    }
}
