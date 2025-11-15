using System.ComponentModel.DataAnnotations;

namespace GymManagementSystem.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email zorunludur")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Beni Hatırla?")]
        public bool RememberMe { get; set; }
    }
}
