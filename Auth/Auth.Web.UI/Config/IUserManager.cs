using System.Threading.Tasks;
using Auth.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Auth.Web.UI.Config
{
    public interface IUserManager
    {
        Task<UserDetails> FindClientByIdAsync(string username);
        Task<UserDetails> FindByEmailAsync(string username);
        Task<UserDetails> FindByUsernameAndApiKey(string username, string apiKey);
        Task<UserDetails> FindByIdAsync(int userId);
        Task<string> GeneratePasswordResetTokenAsync(int userId);
        Task<IdentityResult> ResetPasswordAsync(int userId, string code, string password);
        Task<IdentityResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<IdentityResult> ChangePasswordByAdminAsync(int userId, string newPassword);
        Task<bool> VerifyUserTokenAsync(int userId, string code);
        bool ValidateCredentials(string modelUsername, string modelPassword);
    }
}
