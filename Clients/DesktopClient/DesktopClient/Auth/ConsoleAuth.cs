using DesktopClient.Domain;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using System.Net;

namespace DesktopClient.Auth
{
    public class ConsoleAuth
    {
        private readonly AuthSettings _settings;
        private readonly Func<string> _getBaseToken;
        private readonly string _baseToken;

        public ConsoleAuth(AuthSettings settings, Func<string> getBaseToken)
        {
            _settings = settings;
            _getBaseToken = getBaseToken;
        }

        public ConsoleAuth(AuthSettings settings, string baseToken)
        {
            _settings = settings;
            _baseToken = baseToken;
        }

        private static HttpClientHandler GetDefaultHandler()
        {
            return new HttpClientHandler()
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                Proxy = GetProxy(),
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                {
                    return true;
                }
            };
        }

        private static WebProxy GetProxy()
        {
            return null;
        }

        public async Task<TokenResponse> RequestTokenAsync()
        {
            using (var client = new HttpClient(GetDefaultHandler()))
            {
                var disco = await client.GetDiscoveryDocumentAsync(
                    new DiscoveryDocumentRequest
                    {
                        Address = _settings.AuthServerUrl,
                        Policy =
                            {
                                ValidateIssuerName = false,
                                ValidateEndpoints = false,
                            },
                    }).ConfigureAwait(false);
                if (disco.IsError) throw new Exception(disco.Error);
                var token = _getBaseToken != null ? _getBaseToken.Invoke() : _baseToken;

                var response = await client.RequestTokenAsync(new TokenRequest
                {
                    Address = disco.TokenEndpoint,
                    ClientId = _settings.AuthClientId,
                    GrantType = _settings.AuthClientGrantType,
                    Parameters =
                    {
                        {"scope", _settings.AuthClientScope},
                        {_settings.AuthClientTokenParamName, "Basic " + token}
                    }
                }).ConfigureAwait(false);

                if (response.IsError) throw new Exception(response.Error);
                return response;
            }
        }
    }
}
