using System.Collections.Generic;
using Auth.Domain.Common;
using IdentityModel;
using IdentityServer4.Models;

namespace Auth.Web.UI.Config
{
    public static class Clients
    {
        public static IEnumerable<IdentityServer4.Models.Client> Get()
        {
            var clients = new List<IdentityServer4.Models.Client>();
            foreach (var clientSetting in ProjectSettings.AuthClientSettings)
            {
                var client = new IdentityServer4.Models.Client
                {
                    Enabled = clientSetting.Enabled,
                    ClientName = clientSetting.ClientName,
                    ClientId = clientSetting.ClientId,
                    RedirectUris = clientSetting.RedirectUris,
                    PostLogoutRedirectUris = clientSetting.PostLogoutRedirectUris,
                    AllowedScopes = clientSetting.AllowedScopes,
                    AllowOfflineAccess = clientSetting.AllowOfflineAccess,
                    RequireConsent = clientSetting.RequireConsent,
                    RequirePkce = clientSetting.RequirePkce,
                    RequireClientSecret = clientSetting.RequireClientSecret,
                    AccessTokenLifetime = clientSetting.AccessTokenLifetime,
                    AllowAccessTokensViaBrowser = clientSetting.AllowAccessTokensViaBrowser,
                    AllowedGrantTypes = ParseGrantTypes(clientSetting.AllowedGrantTypes),
                    AccessTokenType = clientSetting.AccessTokenType == "jwt" ? AccessTokenType.Jwt : AccessTokenType.Reference,
                    AllowedCorsOrigins = clientSetting.AllowedCorsOrigins
                };
                clientSetting.ClientSecrets.ForEach(x => client.ClientSecrets.Add(new Secret(x.ToSha256())));
                clients.Add(client);
            }

            return clients;
        }

        private static List<string> ParseGrantTypes(List<string> clientSettingAllowedGrantTypes)
        {
            var result = new List<string>();
            foreach (var item in clientSettingAllowedGrantTypes)
            {
                if (item.ToLower() == "implicit")
                {
                    result.Add("implicit");
                }
                else if (item.ToLower() == "hybrid")
                {
                    result.Add("hybrid");
                }
                else if (item.ToLower() == "authorizationcode")
                {
                    result.Add("authorization_code");
                }
                else if (item.ToLower() == "clientcredentials")
                {
                    result.Add("client_credentials");
                }
                else if (item.ToLower() == "resourceownerpassword")
                {
                    result.Add("password");
                }
                else if (item.ToLower() == "deviceflow")
                {
                    result.Add("urn:ietf:params:oauth:grant-type:device_code");
                }
                else if (item.ToLower() == "implicitandclientcredentials")
                {
                    result.Add("implicit");
                    result.Add("client_credentials");
                }
                else if (item.ToLower() == "code")
                {
                    result.Add("authorization_code");
                }
                else if (item.ToLower() == "codeandclientcredentials")
                {
                    result.Add("authorization_code");
                    result.Add("client_credentials");
                }
                else if (item.ToLower() == "hybridandclientcredentials")
                {
                    result.Add("hybrid");
                    result.Add("client_credentials");
                }
                else if (item.ToLower() == "resourceownerpasswordandclientcredentials")
                {
                    result.Add("password");
                    result.Add("client_credentials");
                }
                else if (item.ToLower() == "delegation")
                {
                    result.Add("delegation");
                }
                else if (item.ToLower() == "customgranttoken")
                {
                    result.Add("custom_grant_token");
                }
            }
            return result;
        }
    }
}
