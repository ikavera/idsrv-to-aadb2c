using WebApi.Domain;

namespace WebApiWithAad.Services.AzureGraph
{
    public interface IAzureGraphApiService
    {
        Task MigrateUsers();
        Task SetUserRequiresMigration();
        Task ResetUserMigrationStatus(string userGuid);
        Task<Microsoft.Graph.User> LoadGraphUser(string userGuid, List<string> fieldsToLoad);
        Task<List<Microsoft.Graph.User>> LoadGraphUsers(List<string> fieldsToLoad);
        Task UpdateGraphUser(string userGuid, UserDetails user);
        Task ChangePassword(UserDetails userGuid, string newPassword, string currentPassword);
        Task<Microsoft.Graph.User> CreateUser(UserDetails baseUser);
        Task<IEnumerable<UserDetails>> FindUsersData(bool isWebAdmin, int currentUserId);
        Task DeleteUser(string userAadB2CGuid);
        Task DisableAllUsers();
        Task ToggleUser(string userAadB2CGuid);
        Task EnableImpersonation(string guid, bool isWebAdmin);
    }
}
