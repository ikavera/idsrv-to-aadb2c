﻿namespace DesktopClient.Domain
{
    public class AppSettings
    {
        public static string AuthServerUrl { get; set; }
        public static string AuthClientId { get; set; }
        public static string AuthClientGrantType { get; set; }
        public static string AuthClientScope { get; set; }
        public static string AuthClientTokenParamName { get; set; }
        public static string ApiUrl { get; set; }
        public static string AuthRedirectUri { get; set; }
        public static string AuthTenant { get; set; }
        public static string AuthB2CHost { get; set; }
        public static string AuthSignInPolicy { get; set; }
    }
}
