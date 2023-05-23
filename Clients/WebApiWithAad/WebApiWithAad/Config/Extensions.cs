using System.Security.Claims;

namespace WebApi.Config
{
    public static class Extensions
    {
        public static T GetUserId<T>(this ClaimsPrincipal principal)
        {
            if (principal == null) throw new ArgumentNullException(nameof(principal));

            var loggedInUserId = principal.FindFirstValue("user_id");

            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(loggedInUserId, typeof(T));
            }

            if (typeof(T) == typeof(int) || typeof(T) == typeof(long))
            {
                return loggedInUserId != null ? (T)Convert.ChangeType(loggedInUserId, typeof(T)) : (T)Convert.ChangeType(0, typeof(T));
            }

            throw new Exception("Invalid type provided");
        }
    }
}
