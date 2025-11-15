using Microsoft.AspNetCore.Identity;

namespace GymManagementSystem.Services
{
    public class TurkishIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError PasswordRequiresNonAlphanumeric()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresNonAlphanumeric),
                Description = "Şifre en az bir özel karakter içermelidir (!@#$%^&* vb.)."
            };
        }

        public override IdentityError PasswordRequiresDigit()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresDigit),
                Description = "Şifre en az bir rakam içermelidir (0-9)."
            };
        }

        public override IdentityError PasswordRequiresLower()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresLower),
                Description = "Şifre en az bir küçük harf içermelidir (a-z)."
            };
        }

        public override IdentityError PasswordRequiresUpper()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresUpper),
                Description = "Şifre en az bir büyük harf içermelidir (A-Z)."
            };
        }

        public override IdentityError PasswordTooShort(int length)
        {
            return new IdentityError
            {
                Code = nameof(PasswordTooShort),
                Description = $"Şifre en az {length} karakter olmalıdır."
            };
        }

        public override IdentityError DuplicateUserName(string? userName)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description = $"'{userName ?? "Bu"}' kullanıcı adı zaten kullanılıyor."
            };
        }

        public override IdentityError DuplicateEmail(string? email)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateEmail),
                Description = $"'{email ?? "Bu"}' e-posta adresi zaten kayıtlı."
            };
        }

        public override IdentityError InvalidEmail(string? email)
        {
            return new IdentityError
            {
                Code = nameof(InvalidEmail),
                Description = $"'{email ?? "Bu"}' geçerli bir e-posta adresi değil."
            };
        }

        public override IdentityError InvalidUserName(string? userName)
        {
            return new IdentityError
            {
                Code = nameof(InvalidUserName),
                Description = $"'{userName ?? "Bu"}' geçerli bir kullanıcı adı değil."
            };
        }

        public override IdentityError PasswordMismatch()
        {
            return new IdentityError
            {
                Code = nameof(PasswordMismatch),
                Description = "Şifre yanlış."
            };
        }

        public override IdentityError DefaultError()
        {
            return new IdentityError
            {
                Code = nameof(DefaultError),
                Description = "Bir hata oluştu."
            };
        }
    }
}
