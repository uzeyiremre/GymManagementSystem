using System.ComponentModel.DataAnnotations;

namespace GymManagementSystem.Models.Entities
{
    // Spor salonu bilgileri
    public class Gym
    {
        [Key]
        public int GymId { get; set; }

        [Required(ErrorMessage = "Salon adı zorunludur")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;  // Varsayılan değer

        [Required]
        [StringLength(200)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public TimeSpan OpeningTime { get; set; }

        [Required]
        public TimeSpan ClosingTime { get; set; }

        public string? Description { get; set; }  // Nullable

        // İlişkili tablolar
        public ICollection<Service>? Services { get; set; }  // Nullable
        public ICollection<Trainer>? Trainers { get; set; }  // Nullable
    }
}
