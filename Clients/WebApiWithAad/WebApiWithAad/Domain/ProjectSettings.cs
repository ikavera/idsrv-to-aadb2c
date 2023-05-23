namespace WebApi.Domain
{
    public class ProjectSettings
    {
        public static List<string> ValidIssuers { get; set; }
        public static string AuthServer { get; set; }
        public static List<string> CorsAllowedDomains { get; set; }
        public static bool IsSwaggerDebug { get; set; }
        public static string ConnectionString { get; set; }
        public static string AadB2CTenantId { get; set; }
        public static string AadB2CClientId { get; set; }
        public static string AadB2CSecretValue { get; set; }
        public static string AadB2CDomain { get; set; }
        public static string AadB2CExtensionAppId { get; set; }
        public static Dictionary<string, string> AadB2CAuthScopes { get; set; }
    }
}
