using WebApi.Domain;
using WebApiWithAad.Domain;

namespace WebApiWithAad.Services.User
{
    public interface IUserService
    {
        string FindAadB2cByUserNameAndKey(string email, string apiKey);
        List<UserDetails> FindAll();
        int GetUserIdByGuid(string loggedInUserGuid);
        List<UserWithAadGuid> GetUserIdsWithGuid();
        List<PortalPermission> GetUserPermissions(int userId);
        List<UserDetails> GetUsersForMigration();
        List<UserDetails> GetUsersWithGuids();
        void UpdateUserAadGuid(int id1, string id2);
    }
}
