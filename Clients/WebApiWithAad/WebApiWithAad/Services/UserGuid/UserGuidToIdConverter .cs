using System.Security.Claims;
using WebApiWithAad.Services.Cache;
using WebApiWithAad.Services.User;

namespace WebApiWithAad.Services.UserGuid
{
    public class UserGuidToIdConverter : IUserGuidToIdConverter
    {
        private readonly IUserService _userService;
        private readonly ICacheService _cacheService;
        private const string USER_GUID_ID_KEY = "UserGuidToId_";

        public UserGuidToIdConverter(IUserService userService, ICacheService cacheService)
        {
            _userService = userService;
            _cacheService = cacheService;
        }

        public int Convert(ClaimsPrincipal principal)
        {
            if (principal == null) throw new ArgumentNullException(nameof(principal));
            var loggedInUserGuid = principal.FindFirstValue("sub");
            if (!string.IsNullOrEmpty(loggedInUserGuid))
            {
                var key = GetUserIdCacheKey(loggedInUserGuid);
                var cached = _cacheService.Get(key);
                if (cached == null)
                {
                    var userId = _userService.GetUserIdByGuid(loggedInUserGuid);
                    _cacheService.Add(key, userId);
                    return userId;
                }

                if (int.TryParse(cached.ToString(), out var cachedUserId))
                {
                    return cachedUserId;
                }
                throw new ArgumentException("AAD B2C converted Guid cached value is not int");
            }

            throw new ArgumentException("AAD B2C Guid is missing or incorrect");
        }

        public void Regenerate(ClaimsPrincipal principal)
        {
            var userId = Convert(principal);
            ((ClaimsIdentity)principal.Identity).AddClaim(new Claim("user_id", userId.ToString()));
            var roles = _userService.GetUserPermissions(userId);
            roles.ForEach(x => ((ClaimsIdentity)principal.Identity).AddClaim(new Claim("role", x.PermissionName)));
        }

        public void RefreshAndRegenerate(ClaimsPrincipal principal)
        {
            var loggedInUserGuid = principal.FindFirstValue("sub");
            var key = GetUserIdCacheKey(loggedInUserGuid);
            _cacheService.Remove(key);
            Regenerate(principal);
        }

        private string GetUserIdCacheKey(string loggedInUserGuid)
        {
            return USER_GUID_ID_KEY + loggedInUserGuid;
        }
    }
}
