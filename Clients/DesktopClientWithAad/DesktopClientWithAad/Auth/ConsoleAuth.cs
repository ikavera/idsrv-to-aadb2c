using DesktopClient.Domain;
using System;
using System.Threading.Tasks;
using System.Net;
using DesktopClientWithAad.Auth;
using System.Collections.Generic;
using System.Security;
using Microsoft.Identity.Client;
using System.Diagnostics;

namespace DesktopClient.Auth
{
    public class ConsoleAuth
    {
        private readonly AuthSettings _settings;
        private readonly string _email;
        private readonly string _apiKey;
        private string _authorityBase;
        private string _authoritySignUpSignIn;

        public ConsoleAuth(AuthSettings settings, Func<(string, string)> getUserAndKey)
        {
            _settings = settings;
            if (getUserAndKey != null)
            {
                (_email, _apiKey) = getUserAndKey.Invoke();
            }
            Init();
        }

        public ConsoleAuth(AuthSettings settings, string email, string apiKey)
        {
            _settings = settings;
            _email = email;
            _apiKey = apiKey;
            Init();
        }

        private void Init()
        {
            _authorityBase = $"https://{_settings.AuthB2CHost}/tfp/{_settings.AuthTenant}/";
            _authoritySignUpSignIn = $"{_authorityBase}{_settings.AuthSignInPolicy}";
        }

        public async Task<AuthToken> RequestTokenAsync()
        {
            var res = new AuthToken();
            try
            {
                var publicClientApp = PublicClientApplicationBuilder.Create(_settings.AuthClientId)
                    .WithB2CAuthority(_authoritySignUpSignIn)
                    .WithRedirectUri(_settings.AuthRedirectUri)
                    .Build();
                SecureString theSecureString = new NetworkCredential("", _apiKey).SecurePassword;
                var scopes = new List<string> { _settings.AuthClientScope };
                var authResult = await publicClientApp
                    .AcquireTokenByUsernamePassword(scopes, _email, theSecureString)
                    .WithB2CAuthority(_authoritySignUpSignIn)
                    .ExecuteAsync().ConfigureAwait(false);
                res.AccessToken = authResult.AccessToken;
                res.ExpiresIn = authResult.ExpiresOn;
            }
            catch (MsalException ex)
            {
                Debug.WriteLine(ex);
                throw new ArgumentException("Wrong auth settings");
            }
            return res;
        }
    }
}
