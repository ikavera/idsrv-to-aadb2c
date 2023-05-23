using System.Security.Claims;

namespace WebApiWithAad.Services.UserGuid
{
    public interface IUserGuidToIdConverter
    {
        int Convert(ClaimsPrincipal principal);
        void Regenerate(ClaimsPrincipal principal);
        void RefreshAndRegenerate(ClaimsPrincipal principal);
    }
}
