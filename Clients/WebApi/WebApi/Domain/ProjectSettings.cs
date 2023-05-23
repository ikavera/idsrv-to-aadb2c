namespace WebApi.Domain
{
    public class ProjectSettings
    {
        public static List<string> ValidIssuers { get; set; }
        public static string AuthServer { get; set; }
        public static List<string> CorsAllowedDomains { get; set; }
        public static bool IsSwaggerDebug { get; set; }
        public static string ConnectionString { get; set; }
    }
}
