using DesktopClient.Auth;
using DesktopClient.Domain;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Windows;

namespace DesktopClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IConfiguration Config { get; private set; }
        public App()
        {
            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: true)
                .Build();

            AppSettings.AuthServerUrl = Config["AuthServerUrl"]!;
            AppSettings.AuthClientId = Config["AuthClientId"]!;
            AppSettings.ApiUrl = Config["ApiUrl"]!;
            AppSettings.AuthTenant = Config["AuthTenant"]!;
            AppSettings.AuthSignInPolicy = Config["AuthSignInPolicy"]!;
            AppSettings.AuthB2CHost = Config["AuthB2CHost"]!;
            AppSettings.AuthRedirectUri = Config["AuthRedirectUri"]!;
            AppSettings.AuthClientScope = Config["AuthClientScope"]!;
            var authSettings = new AuthSettings
            {
                AuthClientId = Config["AuthClientId"].Trim(),
                AuthClientScope = Config["AuthClientScope"].Trim(),
                AuthTenant = Config["AuthTenant"].Trim(),
                AuthSignInPolicy = Config["AuthSignInPolicy"].Trim(),
                AuthB2CHost = Config["AuthB2CHost"].Trim(),
                AuthRedirectUri = Config["AuthRedirectUri"].Trim()
            };
            JwtMapper.SetupJwtSettings(authSettings);
            ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
        }
    }
}
