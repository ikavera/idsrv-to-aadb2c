using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Auth.Domain.Common;
using Shared.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Web.UI.Config
{
    public static class ConfigHelper
    {
        public static void LoadSettings(IConfiguration configuration)
        {
            ProjectSettings.ConnectionString = configuration.GetValue<string>("ConnectionStrings:MainDb");
            ProjectSettings.CertificateName = configuration.GetValue<string>("AppConfiguration:CertificateName");
            ProjectSettings.CertificatePassword = configuration.GetValue<string>("AppConfiguration:CertificatePassword");
            ProjectSettings.UseExplicitAzureKeyVaultCredentials = configuration.GetValue<bool>("AppConfiguration:UseExplicitAzureKeyVaultCredentials");
            if (ProjectSettings.UseExplicitAzureKeyVaultCredentials)
            {
                ProjectSettings.AzureTenantId = configuration.GetValue<string>("AppConfiguration:Azure_TenantId");
                ProjectSettings.AzureClientId = configuration.GetValue<string>("AppConfiguration:Azure_ClientId");
                ProjectSettings.AzureClientSecret = configuration.GetValue<string>("AppConfiguration:Azure_ClientSecret");
                Environment.SetEnvironmentVariable("AZURE_TENANT_ID", ProjectSettings.AzureTenantId);
                Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", ProjectSettings.AzureClientId);
                Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", ProjectSettings.AzureClientSecret);
            }
            ProjectSettings.GrantsToSkipTosCheck = GetGrantsToSkip(configuration);
            AddAuthClients(configuration);            
        }

        private static void AddAuthClients(IConfiguration configuration)
        {
            var authClients = configuration.GetSection("ClientSettings").GetChildren().ToList();
            if (authClients.Any())
            {
                ProjectSettings.AuthClientSettings = new List<ClientSetting>();
                foreach (var authClientSection in authClients)
                {
                    var authClientSettings = authClientSection.GetChildren().ToList();
                    var setting = new ClientSetting
                    {
                        ClientId = authClientSettings.FirstOrDefault(x => x.Key == "ClientId")?.Value,
                        Enabled = bool.Parse(authClientSettings.FirstOrDefault(x => x.Key == "Enabled")?.Value),
                        RequirePkce =
                            bool.Parse(authClientSettings.FirstOrDefault(x => x.Key == "RequirePkce")?.Value),
                        RequireClientSecret =
                            bool.Parse(authClientSettings.FirstOrDefault(x => x.Key == "RequireClientSecret")
                                ?.Value),
                        AllowOfflineAccess =
                            bool.Parse(authClientSettings.FirstOrDefault(x => x.Key == "AllowOfflineAccess")?.Value),
                        RequireConsent =
                            bool.Parse(authClientSettings.FirstOrDefault(x => x.Key == "RequireConsent")?.Value),
                        AllowAccessTokensViaBrowser = bool.Parse(authClientSettings
                            .FirstOrDefault(x => x.Key == "AllowAccessTokensViaBrowser")?.Value),
                        AccessTokenLifetime =
                            int.Parse(authClientSettings.FirstOrDefault(x => x.Key == "AccessTokenLifetime")?.Value),
                        AccessTokenType = authClientSettings.FirstOrDefault(x => x.Key == "AccessTokenType")?.Value,
                        ClientName = authClientSettings.FirstOrDefault(x => x.Key == "ClientName")?.Value,
                        With2FactorOnly = bool.Parse(authClientSettings.FirstOrDefault(x => x.Key == "With2FactorOnly")?.Value)
                    };
                    var postRedirects = authClientSettings.FirstOrDefault(x => x.Key == "PostLogoutRedirectUris");
                    if (postRedirects != null)
                    {
                        var items = postRedirects.GetChildren().ToList();
                        if (items.Any())
                        {
                            foreach (var item in items)
                            {
                                setting.PostLogoutRedirectUris.Add(item.Value);
                            }
                        }
                    }

                    var redirects = authClientSettings.FirstOrDefault(x => x.Key == "RedirectUris");
                    if (redirects != null)
                    {
                        var items = redirects.GetChildren().ToList();
                        if (items.Any())
                        {
                            foreach (var item in items)
                            {
                                setting.RedirectUris.Add(item.Value);
                            }
                        }
                    }

                    var allowedScopes = authClientSettings.FirstOrDefault(x => x.Key == "AllowedScopes");
                    if (allowedScopes != null)
                    {
                        var items = allowedScopes.GetChildren().ToList();
                        if (items.Any())
                        {
                            foreach (var item in items)
                            {
                                setting.AllowedScopes.Add(item.Value);
                            }
                        }
                    }

                    var clientSecrets = authClientSettings.FirstOrDefault(x => x.Key == "ClientSecrets");
                    if (clientSecrets != null)
                    {
                        var items = clientSecrets.GetChildren().ToList();
                        if (items.Any())
                        {
                            foreach (var item in items)
                            {
                                setting.ClientSecrets.Add(item.Value);
                            }
                        }
                    }

                    var allowedGrantTypes = authClientSettings.FirstOrDefault(x => x.Key == "AllowedGrantTypes");
                    if (allowedGrantTypes != null)
                    {
                        var items = allowedGrantTypes.GetChildren().ToList();
                        if (items.Any())
                        {
                            foreach (var item in items)
                            {
                                setting.AllowedGrantTypes.Add(item.Value);
                            }
                        }
                    }

                    var allowedCorsOrigins = authClientSettings.FirstOrDefault(x => x.Key == "AllowedCorsOrigins");
                    if (allowedCorsOrigins != null)
                    {
                        var items = allowedCorsOrigins.GetChildren().ToList();
                        if (items.Any())
                        {
                            foreach (var item in items)
                            {
                                setting.AllowedCorsOrigins.Add(item.Value);
                            }
                        }
                    }
                    ProjectSettings.AuthClientSettings.Add(setting);
                }
            }
        }

        private static List<string> GetGrantsToSkip(IConfiguration configuration)
        {
            var section = configuration.GetSection("GrantsToSkipTosCheck").Get<string[]>();
            if(section == null) return new List<string>();
            return section.ToList();
        }

        public static X509Certificate2 LoadCertificate(IWebHostEnvironment environment)
        {
            var x509KeyStorageFlags = X509KeyStorageFlags.MachineKeySet |
                                      X509KeyStorageFlags.PersistKeySet |
                                      X509KeyStorageFlags.Exportable;

            X509Certificate2 cert = new X509Certificate2(Path.Combine(environment.ContentRootPath, ProjectSettings.CertificateName),
                ProjectSettings.CertificatePassword, x509KeyStorageFlags);
            return cert;
        }

        public static void AddPolicies(IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Web:Admin", policy =>
                    policy.RequireClaim("role", "Web:Admin"));
                options.AddPolicy("Web:User", policy =>
                    policy.RequireClaim("role", "Web:User"));
                options.AddPolicy("Desktop:Admin", policy =>
                    policy.RequireClaim("role", "Desktop:Admin"));
                options.AddPolicy("Desktop:User", policy =>
                    policy.RequireClaim("role", "Desktop:User"));

                var defaultAuthorizationPolicyBuilder =
                    new AuthorizationPolicyBuilder("Bearer", "temp");
                defaultAuthorizationPolicyBuilder = defaultAuthorizationPolicyBuilder.RequireAuthenticatedUser();
                options.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();
            });
        }
    }
}
