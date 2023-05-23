using Azure.Identity;
using Microsoft.Graph;
using System.Diagnostics;
using System.Text;
using WebApi.Domain;
using WebApiWithAad.Domain;
using WebApiWithAad.Services.User;

namespace WebApiWithAad.Services.AzureGraph
{
    public class AzureGraphApiService : IAzureGraphApiService
    {

        private const string USER_IDENTITY_TYPE = "emailAddress";

        private readonly IUserService _userService;

        public AzureGraphApiService(IUserService userService)
        {
            _userService = userService;
        }

        public async Task MigrateUsers()
        {
            GraphServiceClient graphClient = GetGraphClient();
            var users = _userService.GetUsersForMigration()
                .Where(x => !string.IsNullOrEmpty(x.FirstName) && !string.IsNullOrEmpty(x.LastName))
                .ToList();
            var current = 0;
            foreach (var user in users)
            {
                Debug.WriteLine($"Migrating {++current} of {users.Count}: " + user.Email);
                await CreateUserInGraph(user, graphClient, ProjectSettings.AadB2CDomain, true);
            }
        }

        public async Task SetUserRequiresMigration()
        {
            GraphServiceClient graphClient = GetGraphClient();
            var users = _userService.GetUsersWithGuids();
            foreach (var baseUser in users)
            {
                try
                {
                    var user = await graphClient
                        .Users[baseUser.AadB2CGuid].Request().GetAsync();

                    IDictionary<string, object> extensionInstance = new Dictionary<string, object>();
                    extensionInstance.Add(GetExtensionName("requiresMigration"), true);
                    user.AdditionalData = extensionInstance;
                    await graphClient.Users[baseUser.AadB2CGuid]
                        .Request()
                        .UpdateAsync(user);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        public async Task ResetUserMigrationStatus(string userGuid)
        {
            GraphServiceClient graphClient = GetGraphClient();
            IDictionary<string, object> extensionInstance = new Dictionary<string, object>();
            var user = await graphClient
                .Users[userGuid].Request().GetAsync();
            extensionInstance.Add(GetExtensionName("requiresMigration"), true);
            user.AdditionalData = extensionInstance;
            await graphClient.Users[userGuid]
                .Request()
                .UpdateAsync(user);
        }

        public async Task<Microsoft.Graph.User> LoadGraphUser(string userGuid, List<string> fieldsToLoad)
        {
            if (fieldsToLoad.Count == 0)
            {//as example
                fieldsToLoad.Add("id");
                fieldsToLoad.Add(GetExtensionName("requiresMigration"));
                fieldsToLoad.Add("displayName");
                fieldsToLoad.Add("userPrincipalName");
                fieldsToLoad.Add("passwordProfile");
            }
            var toSelect = string.Join(",", fieldsToLoad);
            GraphServiceClient graphClient = GetGraphClient();
            return await graphClient.Users[userGuid]
                .Request()
                .Select(toSelect)
                .GetAsync();
        }

        public async Task<List<Microsoft.Graph.User>> LoadGraphUsers(List<string> fieldsToLoad)
        {
            if (fieldsToLoad.Count == 0)
            {//as example
                fieldsToLoad.Add("id");
                fieldsToLoad.Add(GetExtensionName("requiresMigration"));
                fieldsToLoad.Add("displayName");
                fieldsToLoad.Add("userPrincipalName");
                fieldsToLoad.Add("passwordProfile");
            }
            var toSelect = string.Join(",", fieldsToLoad);
            GraphServiceClient graphClient = GetGraphClient();
            var results = await graphClient.Users.Request().Select(toSelect).GetAsync();

            List<Microsoft.Graph.User> graphUsers = new List<Microsoft.Graph.User>();
            graphUsers.AddRange(results.CurrentPage.OfType<Microsoft.Graph.User>());

            while (results.NextPageRequest != null)
            {
                results = await results.NextPageRequest.GetAsync();
                graphUsers.AddRange(results.CurrentPage.OfType<Microsoft.Graph.User>());
            }
            return graphUsers;
        }

        public async Task UpdateGraphUser(string userGuid, UserDetails user)
        {
            try
            {
                const string toSelect = "GivenName,Surname,CompanyName,Identities,StreetAddress,DisplayName,City,Country,PostalCode,BusinessPhones,FaxNumber,Id,AccountEnabled";
                GraphServiceClient graphClient = GetGraphClient();
                var currentUser = await graphClient
                    .Users[userGuid].Request()
                    .Select(toSelect).GetAsync();
                if (currentUser.Identities == null)
                {
                    currentUser.Identities = new List<ObjectIdentity>
                    {
                        new ObjectIdentity
                        {
                            Issuer = ProjectSettings.AadB2CDomain,
                            IssuerAssignedId = user.Email,
                            SignInType = USER_IDENTITY_TYPE
                        }
                    };
                }
                else
                {
                    currentUser.Identities.First(x => x.SignInType == USER_IDENTITY_TYPE).IssuerAssignedId = user.Email;
                }
                currentUser.GivenName = user.FirstName;
                currentUser.Surname = user.LastName;
                currentUser.DisplayName = $"{user.FirstName} {user.LastName}";
                currentUser.Country = null;
                currentUser.Id = userGuid;
                currentUser.AccountEnabled = true;

                IDictionary<string, object> extensionInstance = new Dictionary<string, object>();
                extensionInstance.Add(GetExtensionName("preferredMfaType"), 0);
                currentUser.AdditionalData = extensionInstance;

                await graphClient.Users[userGuid].Request().UpdateAsync(currentUser);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public async Task ChangePassword(UserDetails user, string newPassword, string currentPassword)
        {
            try
            {
                GraphServiceClient graphClient = GetGraphClient();
                var currentUser = await graphClient
                    .Users[user.AadB2CGuid].Request().GetAsync();
                currentUser.PasswordPolicies = "DisablePasswordExpiration,DisableStrongPassword";
                currentUser.PasswordProfile = new PasswordProfile
                {
                    Password = newPassword,
                    ForceChangePasswordNextSignIn = false
                };
                // https://github.com/Azure-Samples/ms-identity-dotnetcore-b2c-account-management/issues/12
                // grant User administrator role for application(clientId refers on it)
                await graphClient.Users[user.AadB2CGuid].Request().UpdateAsync(currentUser);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public async Task<Microsoft.Graph.User> CreateUser(UserDetails baseUser)
        {
            try
            {
                GraphServiceClient graphClient = GetGraphClient();
                return await CreateUserInGraph(baseUser, graphClient, ProjectSettings.AadB2CDomain, false);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }

        public async Task<IEnumerable<UserDetails>> FindUsersData(bool isWebAdmin, int currentUserId)
        {
            // looks like this approach is too slow
            var internalData = _userService.FindAll().ToList();
            List<UserWithAadGuid> usersWithGuids = _userService.GetUserIdsWithGuid();
            GraphServiceClient graphClient = GetGraphClient();
            const string toSelect = "GivenName,Surname,CompanyName";
            var page = await graphClient.Users.Request()
                .Select(toSelect)
                .GetAsync();
            var aadList = new List<Microsoft.Graph.User>(internalData.Count);
            while (page.NextPageRequest != null)
            {
                page = await page.NextPageRequest
                    .GetAsync();
                aadList.AddRange(page);
            }

            foreach (var userWithGuid in usersWithGuids)
            {
                var record = internalData.FirstOrDefault(x => x.Id == userWithGuid.UserId);
                if (record != null)
                {
                    var aadRecord = aadList.FirstOrDefault(x => x.Id == userWithGuid.AadB2CGuid);
                    if (aadRecord != null)
                    {
                        record.FirstName = aadRecord.GivenName;
                        record.LastName = aadRecord.Surname;
                    }
                }
            }

            return internalData.AsEnumerable();
        }

        public async Task DeleteUser(string userAadB2CGuid)
        {
            try
            {
                GraphServiceClient graphClient = GetGraphClient();
                await graphClient.Users[userAadB2CGuid].Request().DeleteAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public async Task DisableAllUsers()
        {
            GraphServiceClient graphClient = GetGraphClient();
            var users = _userService.GetUsersWithGuids();
            var current = 0;
            foreach (var user in users)
            {
                Debug.WriteLine($"Disabled {++current} of {users.Count}: " + user.Email);
                await ChangeUserStatus(user, graphClient, false);
            }
        }

        public async Task ToggleUser(string userAadB2CGuid)
        {
            GraphServiceClient graphClient = GetGraphClient();
            var user = new UserDetails
            {
                AadB2CGuid = userAadB2CGuid
            };
            await ChangeUserStatus(user, graphClient, false);
        }

        public async Task EnableImpersonation(string guid, bool isWebAdmin)
        {
            try
            {
                var canImpersonateKey = GetExtensionName("can_impersonate");
                List<string> fieldsToLoad = new List<string> { canImpersonateKey };
                var graphUser = await LoadGraphUser(guid, fieldsToLoad);
                if (graphUser.AdditionalData.ContainsKey(canImpersonateKey))
                {
                    var strValue = graphUser.AdditionalData[canImpersonateKey].ToString();
                    var val = !string.IsNullOrEmpty(strValue) && int.Parse(strValue) > 0;
                    if (val && isWebAdmin) return;
                }
                IDictionary<string, object> extensionInstance = new Dictionary<string, object>();
                extensionInstance.Add(canImpersonateKey, isWebAdmin ? "1" : "0");
                graphUser.AdditionalData = extensionInstance;
                GraphServiceClient graphClient = GetGraphClient();
                await graphClient.Users[guid].Request().UpdateAsync(graphUser);
                await Task.Delay(25000);// make a pause, otherwise AAD with throw error
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private async Task ChangeUserStatus(UserDetails user, GraphServiceClient graphClient, bool isEnabled)
        {
            try
            {
                var currentUser = await graphClient
                    .Users[user.AadB2CGuid].Request().GetAsync();
                currentUser.AccountEnabled = isEnabled;
                await graphClient.Users[user.AadB2CGuid].Request().UpdateAsync(currentUser);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private GraphServiceClient GetGraphClient()
        {
            var secretProvider = new ClientSecretCredential(ProjectSettings.AadB2CTenantId,
                ProjectSettings.AadB2CClientId, ProjectSettings.AadB2CSecretValue);
            var scopes = new[] { "https://graph.microsoft.com/.default" };
            return new GraphServiceClient(secretProvider, scopes);
        }

        private async Task<Microsoft.Graph.User> CreateUserInGraph(UserDetails baseUser, GraphServiceClient graphClient, string issuer, bool isMigration)
        {
            try
            {
                var generatedPassword = GeneratePassword();
                var user = new Microsoft.Graph.User
                {
                    AccountEnabled = true,
                    UserType = "guest",
                    DisplayName = baseUser.FirstName + " " + baseUser.LastName,
                    GivenName = baseUser.FirstName,
                    Surname = baseUser.LastName,
                    Country = null,
                    PasswordProfile = new PasswordProfile
                    {
                        ForceChangePasswordNextSignIn = false,
                        Password = generatedPassword
                    },
                    Identities = new List<ObjectIdentity>
                    {
                        new ObjectIdentity
                        {
                            Issuer = issuer,
                            IssuerAssignedId = baseUser.Email,
                            SignInType = USER_IDENTITY_TYPE
                        }
                    }
                };

                IDictionary<string, object> extensionInstance = new Dictionary<string, object>();
                // requiresMigration means that user should be seamlessly migrated from our DB to AAD
                extensionInstance.Add(GetExtensionName("requiresMigration"), isMigration);
                extensionInstance.Add(GetExtensionName("mustResetPassword"), !isMigration);
                extensionInstance.Add(GetExtensionName("preferredMfaType"), 0);
                user.AdditionalData = extensionInstance;

                var tmp = await graphClient.Users
                    .Request()
                    .AddAsync(user);
                _userService.UpdateUserAadGuid(baseUser.Id, tmp.Id);
                user.Id = tmp.Id;
                return user;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return new Microsoft.Graph.User();
        }

        private string GeneratePassword()
        {
            var length = 20;
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }

            res.Append("!?#");
            return res.ToString();
        }

        private string GetExtensionName(string baseName)
        {
            //https://docs.microsoft.com/en-us/azure/active-directory-b2c/user-flow-custom-attributes?pivots=b2c-custom-policy#create-a-custom-attribute-through-azure-portal
            //requirements to extension attributes naming
            return $"extension_{ProjectSettings.AadB2CExtensionAppId.Replace("-", "")}_{baseName}";
        }
    }
}
