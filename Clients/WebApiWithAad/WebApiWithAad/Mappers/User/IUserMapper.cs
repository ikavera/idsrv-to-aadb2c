using WebApi.Domain;
using WebApiWithAad.Domain;

namespace WebApiWithAad.Mappers.User
{
    public interface IUserMapper
    {
        UserDetails FindByUserId(int userId);
        List<UserDetails> FindAll();
        List<PortalPermission> GetUserPermissions(int userId);
        int GetUserIdByGuid(string loggedInUserGuid);
        string FindAadB2cByUserNameAndKey(string email, string apiKey);
        List<UserDetails> GetUsersForMigration();
        List<UserDetails> GetUsersWithGuids();
        void UpdateUserAadGuid(int userId, string aadGuid);
        List<UserWithAadGuid> GetJustUserIdsWithGuids();
    }
}
