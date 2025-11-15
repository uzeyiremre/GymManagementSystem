using System;
using System.ComponentModel.DataAnnotations;

namespace GymManagementSystem.Models.ViewModels
{
    public class CreateAppointmentViewModel
    {
        [Required(ErrorMessage = "Antrenör seçimi zorunludur")]
        [Display(Name = "Antrenör")]
        public int TrainerId { get; set; }

        [Required(ErrorMessage = "Randevu tarihi zorunludur")]
        [Display(Name = "Randevu Tarihi")]
        public DateTime AppointmentDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Notlar (Opsiyonel)")]
        public string? Notes { get; set; }
    }
}
