using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementSystem.Models.Entities
{
    // Hizmet bilgileri
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Hizmet adı zorunludur")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(15, 240, ErrorMessage = "Süre 15-240 dakika arası olmalı")]
        public int DurationMinutes { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 10000)]
        public decimal Price { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Bağlı salon
        public int GymId { get; set; }

        // İlişkiler
        [ForeignKey("GymId")]
        public Gym? Gym { get; set; }
        public ICollection<Appointment>? Appointments { get; set; }
        public ICollection<Trainer>? Trainers { get; set; }
    }
}
