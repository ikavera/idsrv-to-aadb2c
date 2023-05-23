using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Auth.Domain.Users;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using log4net;

namespace Auth.Web.UI.Config
{
    public class DesktopGrantValidator : IExtensionGrantValidator
    {
        private readonly IUserManager _users;

        protected static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public DesktopGrantValidator(IUserManager users)
        {
            _users = users;
        }

        public string GrantType => "custom_grant_token";

        public async Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var userToken = GetAuthHeader(context);

            if (string.IsNullOrEmpty(userToken))
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest);
                return;
            }

            string[] credentials = ExtractCredentials(userToken);
            UserDetails userDetails = null;
            if (credentials != null)
            {
                if (credentials.Length == 2)
                {
                    userDetails = await AuthenticateClient(credentials[0], credentials[1]);
                    if (userDetails == null)
                    {
                        throw new Exception($"User {credentials[0]} with auth token {credentials[1]} not found.");
                    }
                }
            }
            else
            {
                throw new Exception("Invalid Authorization Token");
            }

            if (userDetails != null)
            {
                var sub = userDetails.Id.ToString();
                context.Result = new GrantValidationResult(sub, GrantType, new List<Claim>
                {
                    new Claim(JwtClaimTypes.Email, userDetails.Email ?? ""),
                });
            }
        }

        private string GetAuthHeader(ExtensionGrantValidationContext context)
        {
            return context.Request.Raw.Get("token");
        }

        private string[] ExtractCredentials(string authHeader)
        {
            string[] result = null;
            try
            {
                if (authHeader.StartsWith("Basic"))
                {
                    string token = authHeader.Substring(6).Trim();

                    string plainText = DecodeApiKey(token);

                    string[] parts = plainText.Split(':');

                    if (parts.Length != 2)
                    {
                        throw new Exception("401");
                    }
                    result = parts;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("500");
            }
            return result;
        }

        private string DecodeApiKey(string key)
        {
            try
            {
                var base64EncodedBytes = Convert.FromBase64String(key);
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch (Exception ex)
            {
                Logger.Error("Unknown Error while decrypting API Key: " + key);
                Logger.Error(ex.Message, ex);
                throw new Exception("401");
            }
        }

        private Task<UserDetails> AuthenticateClient(string email, string apiKey)
        {
            try
            {
                return _users.FindByUsernameAndApiKey(email, apiKey);
            }
            catch (Exception ex)
            {
                Logger.Error("Unknown Error while Authenticating User: " + email);
                Logger.Error(ex.Message, ex);
                throw new Exception("Unknown Error while Authenticating User: " + email);
            }
        }
    }
}
