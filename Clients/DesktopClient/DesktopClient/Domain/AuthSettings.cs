namespace DesktopClient.Domain
{
    public class AuthSettings
    {
        public string AuthServerUrl { get; set; }
        public string AuthClientId { get; set; }
        public string AuthClientGrantType { get; set; }
        public string AuthClientScope { get; set; }
        public string AuthClientTokenParamName { get; set; }
    }
}
