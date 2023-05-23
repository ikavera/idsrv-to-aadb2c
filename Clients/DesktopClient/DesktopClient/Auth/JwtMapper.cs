using DesktopClient.Domain;
using System;
using System.Text;
using System.Threading.Tasks;

namespace DesktopClient.Auth
{
    public static class JwtMapper
    {
        private static string _cachedJwt;
        private static ConsoleAuth _auth;

        public static bool IsAuthenticated()
        {
            return _cachedJwt != null;
        }

        public async static Task<string> GetJwtToken()
        {
            if (!string.IsNullOrEmpty(_cachedJwt))
            {
                return _cachedJwt;
            }

            var res = await _auth.RequestTokenAsync();
            _cachedJwt = res.AccessToken;
            return _cachedJwt;
        }


        public async static Task<string> GetJwtToken(AuthSettings authSettings, string email, string apiKey)
        {
            if (!string.IsNullOrEmpty(_cachedJwt))
            {
                return _cachedJwt;
            }

            var baseToken = GetApiKey(email, apiKey);
            _auth = new ConsoleAuth(authSettings, baseToken);
            try
            {
                var res = await _auth.RequestTokenAsync();
                _cachedJwt = res.AccessToken;
            }
            catch (Exception ex)
            {
                return null;
            }
            return _cachedJwt;
        }

        private static string GetApiKey(string email, string apiKey)
        {
            return EncodeTo64(email + ":" + apiKey);
        }

        private static string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes = Encoding.ASCII.GetBytes(toEncode);
            return Convert.ToBase64String(toEncodeAsBytes);
        }

        private static string DecodeFrom64(string encodedData)
        {
            byte[] encodedDataAsBytes = Convert.FromBase64String(encodedData);
            return Encoding.ASCII.GetString(encodedDataAsBytes);
        }
    }
}
