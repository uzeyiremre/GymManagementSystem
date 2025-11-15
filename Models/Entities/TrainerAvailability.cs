using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementSystem.Models.Entities
{
    // Antrenör müsaitlik bilgileri
    public class TrainerAvailability
    {
        [Key]
        public int AvailabilityId { get; set; }

        [Required]
        public int TrainerId { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsAvailable { get; set; } = true;

        // İlişki
        [ForeignKey("TrainerId")]
        public Trainer? Trainer { get; set; }
    }
}
