using System.Collections.Generic;
using System.Linq;
using Dapper;
using Auth.Domain.Users;
using Auth.Mappers.Common;
using Auth.Domain.Common;

namespace Auth.Mappers.Users.Impl
{
    public class UserMapper : AbstractMapper, IUser
    {

        #region SQL Definitions

        private const string SQL_SELECT =
            @" SELECT u.[Id] 
			            ,u.[UserPassword]
			            ,u.[FirstName]
			            ,u.[LastName]
			            ,u.[Email]
                        ,u.[ApiKey]
                        ,u.[AadB2CGuid]
		            FROM [User] u  ";

        private const string SQL_UPDATE =
              @"UPDATE [User]
                    SET [Email] = @email
                        ,[UserPassword] = @userPassword
                        ,[FirstName] = @firstName
                        ,[LastName] = @lastName
                    WHERE [Id] = @userId; ";

        private const string SQL_GET_PERMISSIONS = @"SELECT 
                                pg.GroupName || ':' || pt.TypeName as PermissionName
                            FROM [UserPermissions] up
                            INNER JOIN [PermissionGroup] pg ON pg.Id = up.PermissionGroupId
                            INNER JOIN [PermissionType] pt ON pt.Id = up.PermissionTypeId
                            WHERE [UserId] = @userId";

        #endregion

        public UserMapper()
            : base(ProjectSettings.ConnectionString)
        {
        }

        public UserDetails FindByUsername(string email)
        {
            string where = @"WHERE u.[Email] = @userName";
            string sql = string.Concat(SQL_SELECT, where);
            var parameters = new DynamicParameters();
            parameters.Add("@userName", email);
            return FindOne<UserDetails>(sql, parameters);
        }

        public UserDetails FindByUsernameAndApiKey(string email, string apiKey)
        {
            string where = @" 
                            WHERE u.[Email] = @userName 
                              AND u.ApiKey = @apiKey;";
            string sql = string.Concat(SQL_SELECT, where);
            var parameters = new DynamicParameters();
            parameters.Add("@userName", email);
            parameters.Add("@apiKey", apiKey);
            return FindOne<UserDetails>(sql, parameters);
        }

        public UserDetails FindByUserId(int userId)
        {
            string where = @"WHERE u.[Id] = @UserId";
            string sql = string.Concat(SQL_SELECT, where);
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);
            return FindOne<UserDetails>(sql, parameters);
        }

        public void Update(UserDetails user)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@userId", user.Id);
            parameters.Add("@email", user.Email);
            parameters.Add("@userPassword", user.UserPassword);
            parameters.Add("@firstName", user.FirstName);
            parameters.Add("@lastName", user.LastName);
            Execute(SQL_UPDATE, parameters);
        }

        public IList<PortalPermission> GetUserPermissions(int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@userId", userId);
            var result = FindMany<PortalPermission>(SQL_GET_PERMISSIONS, parameters)
                .ToList();
            return result;
        }

        public void InsertPasswordReset(UserPasswordReset userPasswordReset)
        {
            throw new System.NotImplementedException();
        }

        public List<UserPasswordReset> GetPasswordResetData(int userId)
        {
            throw new System.NotImplementedException();
        }

        public void MarkCodeAsUsed(string code, int userId)
        {
            throw new System.NotImplementedException();
        }
    }
}
