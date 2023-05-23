using Auth.Mappers.Users;
using Auth.Mappers.Users.Impl;

namespace Auth.Mappers
{
    public class MapperRegistry
    {
        #region Singleton Constructor
        private static MapperRegistry _instance;
        public static MapperRegistry Instance => _instance ??= new MapperRegistry();

        private MapperRegistry()
        {
        }
        #endregion

        #region Users

        private IUser _userMapper;
        public IUser UserMapper => _userMapper ??= new UserMapper();

        #endregion

    }
}
