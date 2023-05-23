using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Auth.Mappers;
using Auth.Service.Cache;
using Shared.Domain.Common;
using Microsoft.AspNetCore.Http;
using Auth.Domain.Users;
using Auth.Service.Encryption.Impl;

namespace Auth.Web.UI.Config
{
    public class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly ICacheService _cacheService;
        protected readonly IHttpContextAccessor _httpContextAccessor;

        public ResourceOwnerPasswordValidator(ICacheService cacheService, IHttpContextAccessor httpContextAccessor)
        {
            _cacheService = cacheService;
            _httpContextAccessor = httpContextAccessor;
        }

        protected static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(ResourceOwnerPasswordValidator));
        //this is used to validate your user account with provided grant at /connect/token
        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            try
            {
                //get your user model from db (by username - in my case its email)
                var user = MapperRegistry.Instance.UserMapper.FindByUsername(context.UserName);
                if (user != null)
                {
                    //check if password match - remember to hash password if stored as hash in db
                    if (user.UserPassword == context.Password || PasswordStorage.VerifyPassword(context.Password, user.UserPassword))
                    {
                        var tmp = new ProfileService(_cacheService, _httpContextAccessor);
                        user.UserPermissions = tmp.GetUserPermissions(user.Id);
                        //set the result
                        context.Result = new GrantValidationResult(
                            subject: user.Id.ToString(),
                            authenticationMethod: "custom",
                            claims: GetUserClaims(user, _httpContextAccessor));

                        return;
                    }

                    context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Incorrect password");
                    return;
                }

                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "User does not exist.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                context.Result =
                    new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Invalid username or password");
            }
        }

        //build claims array from user data
        public static Claim[] GetUserClaims(UserDetails user, IHttpContextAccessor httpContextAccessor, bool regenerate = false)
        {
            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim("user_id", user.Id.ToString()),
                    new Claim(JwtClaimTypes.Name,
                        (!string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName))
                            ? (user.FirstName + " " + user.LastName)
                            : ""),
                    new Claim(JwtClaimTypes.GivenName, user.FirstName ?? ""),
                    new Claim(JwtClaimTypes.FamilyName, user.LastName ?? ""),
                    new Claim(JwtClaimTypes.Email, user.Email ?? ""),
                    new Claim("tos_accepted", "true"),
                    new Claim(JwtClaimTypes.Subject, user.Id.ToString()),
                };
                if (regenerate)
                {
                    claims.Remove(claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Name));
                    claims.Add(new Claim(JwtClaimTypes.Name, user.Email));
                    claims.Add(new Claim(JwtClaimTypes.AuthenticationMethod, "pwd"));
                    claims.Add(new Claim(JwtClaimTypes.IdentityProvider, "local"));
                    claims.Add(new Claim(JwtClaimTypes.AuthenticationTime, TimeEpochUtility.UtcEpochFromDate(DateTime.Now).ToString()));
                    claims.Add(new Claim(JwtClaimTypes.AuthenticationContextClassReference, "impersonate"));
                }

                user.UserPermissions.ForEach(x => claims.Add(new Claim(JwtClaimTypes.Role, x.PermissionName)));

                return claims.ToArray();
            }
            return null;
        }
    }
}
