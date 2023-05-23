using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Auth.Web.UI.Filters
{
    public class MultiplePoliciesAuthorizeFilter : IAsyncAuthorizationFilter
    {
        private readonly IAuthorizationService _authorization;
        public string Policies { get; private set; }
        public bool IsAnd { get; private set; }

        protected static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

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
                Logger.Error(e);
                context.Result = new ForbidResult();
            }
        }
    }
}
