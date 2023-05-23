namespace DesktopClient.Domain
{
    public class AuthSettings
    {
        public string AuthClientId { get; set; }
        public string AuthClientScope { get; set; }
        public string AuthRedirectUri { get; set; }
        public string AuthTenant { get; set; }
        public string AuthB2CHost { get; set; }
        public string AuthSignInPolicy { get; set; }
    }
}
