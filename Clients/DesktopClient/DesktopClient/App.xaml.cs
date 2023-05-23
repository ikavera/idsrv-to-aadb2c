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
            AppSettings.AuthClientGrantType = Config["AuthClientGrantType"]!;
            AppSettings.AuthClientScope = Config["AuthClientScope"]!;
            AppSettings.AuthClientTokenParamName = Config["AuthClientTokenParamName"]!;
            AppSettings.ApiUrl = Config["ApiUrl"]!;
            ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
        }
    }
}
