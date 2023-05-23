using WebApi.Domain;
using WebApiWithAad.Domain;
using WebApiWithAad.Mappers.User;

namespace WebApiWithAad.Services.User
{
    public class UserService : IUserService
    {
        private readonly IUserMapper _userMapper;

        public UserService(IUserMapper userMapper)
        {
            _userMapper = userMapper;
        }

        public string FindAadB2cByUserNameAndKey(string email, string apiKey)
        {
            return _userMapper.FindAadB2cByUserNameAndKey(email, apiKey);
        }

        public List<UserDetails> FindAll()
        {
            return _userMapper.FindAll();
        }

        public int GetUserIdByGuid(string loggedInUserGuid)
        {
            return _userMapper.GetUserIdByGuid(loggedInUserGuid);
        }

        public List<UserWithAadGuid> GetUserIdsWithGuid()
        {
            return _userMapper.GetJustUserIdsWithGuids();
        }

        public List<PortalPermission> GetUserPermissions(int userId)
        {
            return _userMapper.GetUserPermissions(userId);
        }

        public List<UserDetails> GetUsersForMigration()
        {
            return _userMapper.GetUsersForMigration();
        }

        public List<UserDetails> GetUsersWithGuids()
        {
            return _userMapper.GetUsersWithGuids();
        }

        public void UpdateUserAadGuid(int userId, string aadGuid)
        {
            _userMapper.UpdateUserAadGuid(userId, aadGuid);
        }
    }
}
