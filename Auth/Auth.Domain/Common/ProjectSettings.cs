using Shared.Domain.Common;
using System.Collections.Generic;

namespace Auth.Domain.Common
{
    public static class ProjectSettings
    {
        public static string ConnectionString { get; set; }
        public static List<ClientSetting> AuthClientSettings { get; set; }
        public static List<string> GrantsToSkipTosCheck { get; set; }
        public static string CertificateName { get; set; }
        public static string CertificatesLink { get; set; }
        public static string CertificatePassword { get; set; }
        public static bool UseExplicitAzureKeyVaultCredentials { get; set; }
        public static string AzureTenantId { get; set; }
        public static string AzureClientId { get; set; }
        public static string AzureClientSecret { get; set; }

    }
}
