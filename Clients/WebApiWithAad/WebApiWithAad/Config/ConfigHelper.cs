using WebApi.Domain;

namespace WebApi.Config
{
    public class ConfigHelper
    {
        public static void LoadSettings(IConfiguration configuration)
        {
            ProjectSettings.ConnectionString = configuration.GetValue<string>("ConnectionStrings:MainDb");
            ProjectSettings.CorsAllowedDomains = new List<string>();
            configuration.GetSection("CorsAllowedDomains").AsEnumerable().ToList().ForEach(x =>
            {
                if (x.Value != null)
                {
                    ProjectSettings.CorsAllowedDomains.Add(x.Value);
                }
            });
            ProjectSettings.AuthServer = configuration.GetValue<string>("AuthServer");
            ProjectSettings.IsSwaggerDebug = configuration.GetValue<bool>("IsSwaggerDebug");
            ProjectSettings.ValidIssuers = new List<string>();
            configuration.GetSection("ValidIssuers").AsEnumerable().ToList().ForEach(x =>
            {
                if (x.Value != null)
                {
                    ProjectSettings.ValidIssuers.Add(x.Value);
                }
            });

            ProjectSettings.AadB2CTenantId = configuration.GetValue<string>("AzureAdB2CGraphApp:TenantId");
            ProjectSettings.AadB2CClientId = configuration.GetValue<string>("AzureAdB2CGraphApp:ClientId");
            ProjectSettings.AadB2CSecretValue = configuration.GetValue<string>("AzureAdB2CGraphApp:SecretValue");
            ProjectSettings.AadB2CDomain = configuration.GetValue<string>("AzureAdB2CGraphApp:Domain");
            ProjectSettings.AadB2CExtensionAppId = configuration.GetValue<string>("AzureAdB2CGraphApp:B2CExtensionAppId");
            ProjectSettings.AadB2CAuthScopes = new Dictionary<string, string>();
            configuration.GetSection("Scopes:Auth").AsEnumerable().ToList().ForEach(x =>
            {
                if (x.Value != null)
                {
                    ProjectSettings.AadB2CAuthScopes.Add(x.Value, x.Value.Split("/").Last());
                }
            });
        }
    }
}
