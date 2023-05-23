using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace WebApi.Filters
{
    public class MultiplePoliciesAuthorizeFilter : IAsyncAuthorizationFilter
    {
        private readonly IAuthorizationService _authorization;
        public string Policies { get; }
        public bool IsAnd { get; }

        public MultiplePoliciesAuthorizeFilter(string policies, bool isAnd, IAuthorizationService authorization)
        {
            Policies = policies;
            IsAnd = isAnd;
            _authorization = authorization;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                return;
            }

            var policies = Policies.Split(";").ToList();
            try
            {
                if (IsAnd)
                {
                    foreach (var policy in policies)
                    {
                        var authorized = await _authorization.AuthorizeAsync(context.HttpContext.User, policy);
                        if (!authorized.Succeeded)
                        {
                            context.Result = new ForbidResult();
                            return;
                        }

                    }
                }
                else
                {
                    foreach (var policy in policies)
                    {
                        var authorized = await _authorization.AuthorizeAsync(context.HttpContext.User, policy);
                        if (authorized.Succeeded)
                        {
                            return;
                        }

                    }
                    context.Result = new ForbidResult();
                }

            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                context.Result = new ForbidResult();
            }
        }
    }
}
