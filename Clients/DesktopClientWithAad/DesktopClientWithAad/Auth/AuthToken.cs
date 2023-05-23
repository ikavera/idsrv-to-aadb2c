using System;

namespace DesktopClientWithAad.Auth
{
    public class AuthToken
    {
        public string AccessToken { get; set; }
        public DateTimeOffset ExpiresIn { get; set; }
    }
}
