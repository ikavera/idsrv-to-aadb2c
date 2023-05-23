using Microsoft.AspNetCore.Authentication.JwtBearer;
using WebApiWithAad.Services.UserGuid;

namespace WebApiWithAad.Config
{
    public class AuthHelper
    {
        private readonly IUserGuidToIdConverter _converter;

        public AuthHelper(IUserGuidToIdConverter converter)
        {
            _converter = converter;
        }

        public Task SecurityTokenValidated(TokenValidatedContext context)
        {
            return Task.Run(async () =>
            {
                _converter.Regenerate(context.Principal);
            });
        }

        public Task MessageRecieved(MessageReceivedContext arg)
        {
            if (arg.Request.Method.Equals("GET") && arg.Request.Query.ContainsKey("access_token"))
                arg.Token = arg.Request.Query["access_token"];
            return Task.CompletedTask;
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
