using System.Collections.Generic;
using IdentityServer4.Models;

namespace Auth.Web.UI.Config
{
    public static class Config
    {

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
            };
        }

        public static IEnumerable<ApiScope> GetScopes()
        {
            return new List<ApiScope>
            {
                new ApiScope("api1", "My API"),
                new ApiScope("demo_api", "Demo API with Swagger")
            };
        }
    }
}
