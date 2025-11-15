using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagementSystem.Models.Entities
{
    // Randevu bilgileri
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [NotMapped]
        public int Id
        {
            get => AppointmentId;
            set => AppointmentId = value;
        }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public string? Notes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ConfirmedAt { get; set; }

        // Bağlantılar
        public int MemberId { get; set; }
        public int TrainerId { get; set; }
        public int? ServiceId { get; set; }

        // İlişkiler
        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        [ForeignKey("TrainerId")]
        public Trainer? Trainer { get; set; }

        [ForeignKey("ServiceId")]
        public Service? Service { get; set; }
    }
}
