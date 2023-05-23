using System;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DesktopClient.Domain;
using DesktopClient.Auth;
using System.IO.Compression;
using System.Net.Http.Headers;

namespace DesktopClient.Services
{
    public static class WebApiService
    {
        private static string ServiceBaseUrl => AppSettings.ApiUrl;
        private static readonly TimeSpan TIMEOUT = new TimeSpan(0, 20, 0); // 20 minutes

        private static HttpClientHandler GetDefaultHandler()
        {
            return new HttpClientHandler()
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                Proxy = GetProxy(),
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { 
                    return true; 
                }
            };
        }

        private static WebProxy GetProxy()
        {
            return null;
        }

        public static async Task<int> GetUsersCount()
        {
            var uri = ServiceBaseUrl + "api/Restricted/GetUsersCount";
            HttpClientHandler clientHandler = GetDefaultHandler();
            using var httpClient = new HttpClient(clientHandler)
            {
                BaseAddress = new Uri(ServiceBaseUrl)
            };
            httpClient.Timeout = TIMEOUT;
            var request = await GetRequestMessage(HttpMethod.Get, uri, null);
            var response = httpClient.Send(request);
            using var reader = new StreamReader(response.Content.ReadAsStream());
            var json = reader.ReadToEnd();
            return int.Parse(json);
        }

        private async static Task<HttpRequestMessage> GetRequestMessage(HttpMethod method, string uri, string json)
        {
            var webRequest = new HttpRequestMessage(method, uri);
            if (!string.IsNullOrEmpty(json))
            {
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                MemoryStream ms = new MemoryStream();
                using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    gzip.Write(jsonBytes, 0, jsonBytes.Length);
                }
                ms.Position = 0;
                webRequest.Content = new StreamContent(ms);
                webRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                webRequest.Content.Headers.ContentEncoding.Add("gzip");
            }
            webRequest.Headers.Add("Authorization", "Bearer " + await JwtMapper.GetJwtToken());
            return webRequest;
        }
    }
}
