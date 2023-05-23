using Auth.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Auth.Service.Encryption.Impl
{
    public class PasswordStorage : PasswordHasher<UserDto>
    {
        public override string HashPassword(UserDto user, string password)
        {
            var hashed = CreateHash(password);
            return hashed;
        }

        public override PasswordVerificationResult VerifyHashedPassword(UserDto user, string hashedPassword, string providedPassword)
        {
            if (VerifyPassword(providedPassword, hashedPassword))
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }

            return PasswordVerificationResult.Failed;
        }

        public static string CreateHash(string password)
        {
            return password;
        }

        public static bool VerifyPassword(string password, string goodHash)
        {
            return password == goodHash;
        }
    }
}
