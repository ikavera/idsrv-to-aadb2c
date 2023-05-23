using System.Collections.Generic;

namespace Shared.Domain.Common
{
    public class ClientSetting
    {
        public bool Enabled { get; set; }
        public string ClientName { get; set; }
        public string ClientId { get; set; }
        public string AccessTokenType { get; set; }
        public List<string> ClientSecrets { get; set; }
        public List<string> RedirectUris { get; set; }
        public List<string> PostLogoutRedirectUris { get; set; }
        public List<string> AllowedScopes { get; set; }
        public List<string> AllowedGrantTypes { get; set; }
        public List<string> AllowedCorsOrigins { get; set; }
        public bool RequirePkce { get; set; }
        public bool RequireClientSecret { get; set; }
        public bool AllowOfflineAccess { get; set; }
        public bool RequireConsent { get; set; }
        public bool AllowAccessTokensViaBrowser { get; set; }
        public bool With2FactorOnly { get; set; }
        public int AccessTokenLifetime { get; set; }

        public ClientSetting()
        {
            ClientSecrets = new List<string>();
            RedirectUris = new List<string>();
            PostLogoutRedirectUris = new List<string>();
            AllowedScopes = new List<string>();
            AllowedGrantTypes = new List<string>();
            AllowedCorsOrigins = new List<string>();
        }
    }
}
