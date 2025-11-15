using System.ComponentModel.DataAnnotations;

namespace GymManagementSystem.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ad zorunludur")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad zorunludur")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email zorunludur")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre minimum 6 karakter")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor")]
        public string? ConfirmPassword { get; set; }
    }
}
