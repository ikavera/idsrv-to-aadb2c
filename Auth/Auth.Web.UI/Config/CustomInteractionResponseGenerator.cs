using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Auth.Domain.Users;

namespace Auth.Web.UI.Config
{
    public class CustomInteractionResponseGenerator : AuthorizeInteractionResponseGenerator
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserManager _userManager;

        public CustomInteractionResponseGenerator(ISystemClock clock,
            ILogger<AuthorizeInteractionResponseGenerator> logger,
            IConsentService consent,
            IProfileService profile,
            IHttpContextAccessor httpContextAccessor,
            IUserManager userManager)
            : base(clock, logger, consent, profile)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public override async Task<InteractionResponse> ProcessInteractionAsync(ValidatedAuthorizeRequest request, ConsentResponse consent = null)
        {
            var processImpersonateRequest = true;
            var acrValues = request.GetAcrValues().ToList();
            if (acrValues.Count == 0)
            {
                processImpersonateRequest = false;
            }

            var impersonateId = acrValues.FirstOrDefault(x => x.Contains("impersonateId:"));
            if (impersonateId == null)
            {
                processImpersonateRequest = false;
            }

            if (processImpersonateRequest)
            {
                var currentUser = await _userManager.FindByEmailAsync(request.Subject.Identity.Name);
                if (currentUser == null) return await base.ProcessInteractionAsync(request, consent);
                var hasSysAdminPermission = IsUserHasWebAdminPermissions(currentUser);
                if (!hasSysAdminPermission) return await base.ProcessInteractionAsync(request, consent);
                var impersonateUserId = impersonateId.Split(':')[1];
                UserDetails user = await _userManager.FindByIdAsync(int.Parse(impersonateUserId));
                if(IsUserHasWebAdminPermissions(user)) return await base.ProcessInteractionAsync(request, consent);
                var claimPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
                    ResourceOwnerPasswordValidator.GetUserClaims(user, _httpContextAccessor, true).ToList(), "IdentityServer4"));
                request.Subject = claimPrincipal;
                var isUser = new IdentityServerUser(user.Id.ToString())
                {
                    DisplayName = user.Email
                };

                var props = new AuthenticationProperties
                {
                    Items =
                    {
                        {"impersonated","true"}
                    }
                };
                await _httpContextAccessor.HttpContext.SignInAsync(isUser, props);

                return new InteractionResponse
                {
                    IsLogin = false,
                    IsConsent = false
                };
            }

            return await base.ProcessInteractionAsync(request, consent);
        }

        private bool IsUserHasWebAdminPermissions(UserDetails user)
        {
            return user.UserPermissions.FirstOrDefault(x => x.PermissionName == "Web:Admin") != null;
        }
    }
}
