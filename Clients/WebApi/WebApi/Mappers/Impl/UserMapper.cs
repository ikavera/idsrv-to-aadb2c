using Dapper;
using WebApi.Domain;
using WebApi.Mappers.Common;

namespace WebApi.Mappers.Impl
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
    }
}
