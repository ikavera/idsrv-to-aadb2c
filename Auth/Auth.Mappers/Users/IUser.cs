using System.Collections.Generic;
using Auth.Domain.Users;

namespace Auth.Mappers.Users
{
    public interface IUser
    {
        UserDetails FindByUsername(string email);
        UserDetails FindByUsernameAndApiKey(string email, string apiKey);
        void Update(UserDetails user);
        IList<PortalPermission> GetUserPermissions(int userId);
        UserDetails FindByUserId(int userId);
        void InsertPasswordReset(UserPasswordReset userPasswordReset);
        List<UserPasswordReset> GetPasswordResetData(int userId);
        void MarkCodeAsUsed(string code, int userId);
    }
}
