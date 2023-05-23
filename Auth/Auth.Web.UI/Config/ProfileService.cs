using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Auth.Domain.Common;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Auth.Mappers;
using Auth.Service.Cache;
using IdentityModel;
using log4net;
using Microsoft.AspNetCore.Http;
using Auth.Domain.Users;

namespace Auth.Web.UI.Config
{
    public class ProfileService : IProfileService
    {
        private readonly ICacheService _cacheService;
        protected static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected readonly IHttpContextAccessor _httpContextAccessor;

        public ProfileService(ICacheService cacheService, IHttpContextAccessor httpContextAccessor)
        {
            _cacheService = cacheService;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            try
            {
                var user = GetUser(context.Subject);
                user.UserPermissions = GetUserPermissions(user.Id);
                context.IssuedClaims = ResourceOwnerPasswordValidator.GetUserClaims(user, _httpContextAccessor).ToList();
                AddTwoFactorClaim(context.IssuedClaims, context);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Task.FromResult(true);
            }
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            var user = GetUser(context.Subject);
            if (user != null)
            {
                context.IsActive = true;
            }
            else
            {
                context.IsActive = context.Subject.Identity.IsAuthenticated;
            }
            return Task.FromResult(true);
        }

        private bool SkipTosCheck(IsActiveContext context)
        {
            Logger.Error("SkipTosCheck");
            var currentGrant = context.Subject.Claims.FirstOrDefault(x => x.Type == "amr");
            var acr = context.Subject.Claims.FirstOrDefault(x => x.Type == "acr");
            if (acr is { Value: "impersonate" })
            {
                return true;
            }
            if (currentGrant != null)
            {
                return ProjectSettings.GrantsToSkipTosCheck.Contains(currentGrant.Value) || context.Caller == "AuthorizationCodeValidation";
            }

            return false;
        }

        public List<PortalPermission> GetUserPermissions(int userId)
        {
            var key = "userPermissions_" + userId;
            var cached = _cacheService.GetItem(key) as List<PortalPermission>;
            if (cached == null)
            {
                var permissions = MapperRegistry.Instance.UserMapper.GetUserPermissions(userId).ToList();
                _cacheService.AddItem(key, permissions, new TimeSpan(0, 0, 0, 10));
                cached = permissions;
            }

            return cached;
        }

        private UserDetails GetUser(ClaimsPrincipal contextSubject)
        {
            // cache user data for 20 seconds to prevent multiple DB calls on token silent renewal
            string userId = contextSubject.Claims.First(c => c.Type == "sub").Value;
            var key = "cachedUserData_" + userId;
            var cachedData = _cacheService.GetItem(key) as UserDetails;
            if (cachedData == null)
            {
                var email = contextSubject.Identity.Name;
                if (email == null || !email.Contains("@"))
                {
                    var emailClaim = contextSubject.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Email);
                    if (emailClaim != null)
                    {
                        email = emailClaim.Value;
                    }
                }

                if (email != null)
                {
                    var user = MapperRegistry.Instance.UserMapper.FindByUsername(email);
                    _cacheService.AddItem(key, user, new TimeSpan(0, 0, 20));
                    cachedData = user;
                }
                else
                {
                    var user = MapperRegistry.Instance.UserMapper.FindByUserId(int.Parse(userId));
                    _cacheService.AddItem(key, user, new TimeSpan(0, 0, 20));
                    cachedData = user;
                }
            }

            return cachedData;
        }

        private void AddTwoFactorClaim(List<Claim> contextIssuedClaims, ProfileDataRequestContext context)
        {
            var twoFaClaim = context.Subject.Claims.FirstOrDefault(x => x.Type == GlobalConstants.TWO_FA_CLAIM_NAME);
            contextIssuedClaims.Add(twoFaClaim ?? new Claim(GlobalConstants.TWO_FA_CLAIM_NAME, "False"));
        }
    }
}
