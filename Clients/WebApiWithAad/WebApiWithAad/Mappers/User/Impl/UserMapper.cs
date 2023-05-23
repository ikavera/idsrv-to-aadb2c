using Dapper;
using WebApi.Domain;
using WebApi.Mappers.Common;
using WebApiWithAad.Domain;

namespace WebApiWithAad.Mappers.User.Impl
{
    public class UserMapper : AbstractMapper, IUserMapper
    {
        private const string SQL_SELECT =
            @" SELECT u.[Id] 
			            ,u.[UserPassword]
			            ,u.[FirstName]
			            ,u.[LastName]
			            ,u.[Email]
                        ,u.[ApiKey]
                        ,u.[AadB2CGuid]
		            FROM [User] u  ";

        private const string SQL_GET_PERMISSIONS = @"SELECT 
                                pg.GroupName || ':' || pt.TypeName as PermissionName
                            FROM [UserPermissions] up
                            INNER JOIN [PermissionGroup] pg ON pg.Id = up.PermissionGroupId
                            INNER JOIN [PermissionType] pt ON pt.Id = up.PermissionTypeId
                            WHERE [UserId] = @userId";

        private const string SQL_USER_GUID_TO_ID = @"SELECT [Id] FROM [User] WHERE AadB2CGuid = @userGuid ";

        private const string SQL_SELECT_AADB2C_ID_BY_USERNAME_AND_APIKEY =
            @"SELECT AadB2CGuid
                      FROM [User]
                      WHERE [Email] = @userName AND ApiKey = @apiKey ";

        private const string SQL_UPDATE_GUID = "UPDATE [User] SET [AadB2CGuid] = @aadGuid WHERE [Id] = @userId ";

        private const string SQL_SELECT_AADB2C_IDS_WITH_IDS =
            @"SELECT AadB2CGuid, Id
                      FROM [User] ";

        public UserMapper() : base(ProjectSettings.ConnectionString)
        {
        }

        public List<UserDetails> FindAll()
        {
            return FindMany<UserDetails>(SQL_SELECT, null);
        }

        public UserDetails FindByUserId(int userId)
        {
            string where = @"WHERE u.[Id] = @UserId";
            string sql = string.Concat(SQL_SELECT, where);
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);
            return FindOne<UserDetails>(sql, parameters);
        }

        public int GetUserIdByGuid(string loggedInUserGuid)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@userGuid", loggedInUserGuid);
            return FindOne<int>(SQL_USER_GUID_TO_ID, parameters);
        }

        public string FindAadB2cByUserNameAndKey(string email, string apiKey)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@userName", email);
            parameters.Add("@apiKey", apiKey);
            return FirstOrDefault<string>(SQL_SELECT_AADB2C_ID_BY_USERNAME_AND_APIKEY, parameters);
        }

        public List<PortalPermission> GetUserPermissions(int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@userId", userId);
            var result = FindMany<PortalPermission>(SQL_GET_PERMISSIONS, parameters).ToList();
            return result;
        }

        public List<UserDetails> GetUsersForMigration()
        {
            string where = @"WHERE u.[AadB2CGuid] IS NULL";
            string sql = string.Concat(SQL_SELECT, where);
            return FindMany<UserDetails>(sql, null);
        }

        public List<UserDetails> GetUsersWithGuids()
        {
            string where = @"WHERE u.[AadB2CGuid] IS NOT NULL";
            string sql = string.Concat(SQL_SELECT, where);
            return FindMany<UserDetails>(sql, null);
        }

        public void UpdateUserAadGuid(int userId, string aadGuid)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@userId", userId);
            parameters.Add("@aadGuid", aadGuid);
            Execute(SQL_UPDATE_GUID, parameters);
        }

        public List<UserWithAadGuid> GetJustUserIdsWithGuids()
        {
            return FindMany<UserWithAadGuid>(SQL_SELECT_AADB2C_IDS_WITH_IDS);
        }
    }
}
