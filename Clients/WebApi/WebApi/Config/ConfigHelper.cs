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
        }

        public static void AddPolicies(IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Web:Admin", policy =>
                    policy.RequireClaim("role", "Web:Admin"));
                options.AddPolicy("Web:User", policy =>
                    policy.RequireClaim("role", "Web:User"));
                options.AddPolicy("Desktop:Admin", policy =>
                    policy.RequireClaim("role", "Desktop:Admin"));
                options.AddPolicy("Desktop:User", policy =>
                    policy.RequireClaim("role", "Desktop:User"));
            });
        }
    }
}
