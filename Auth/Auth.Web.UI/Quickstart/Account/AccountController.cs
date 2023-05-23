// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Auth.Service.Cache;
using IdentityServer4;
using log4net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Auth.Domain.Users;
using Auth.Service.Encryption.Impl;
using Auth.Web.UI.Config;
using Auth.Web.UI.Quickstart.Account;
using Shared.Domain.Users;

namespace IdentityServer
{
    /// <summary>
    /// This sample controller implements a typical login/logout/provision workflow for local and external accounts.
    /// The login service encapsulates the interactions with the user data store. This data store is in-memory only and cannot be used for production!
    /// The interaction service provides a way for the UI to communicate with identityserver for validation and context retrieval
    /// </summary>
    [SecurityHeaders]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IUserManager _users;
        private readonly IWebHostEnvironment _environment;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IAuthorizationService _authorization;
        private readonly ICacheService _cacheService;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        protected static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        readonly IStringLocalizer _localizer;

        public AccountController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            IUserManager users,
            IStringLocalizerFactory factory,
            IWebHostEnvironment environment,
            IWebHostEnvironment hostingEnvironment,
            IAuthorizationService authorization,
            ICacheService cacheService)
        {
            _users = users;
            _environment = environment;
            _hostingEnvironment = hostingEnvironment;
            _authorization = authorization;
            _cacheService = cacheService;

            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;

            _localizer = factory.Create("Views.Account.Login", Assembly.GetExecutingAssembly().GetName().Name);
        }

        [HttpPost]
        public async Task<IActionResult> LoginValidation([FromBody] MigrationLoginValidationModel model)
        {
            Logger.Error("LoginValidation called");
            if (!ModelState.IsValid)
            {
                Logger.Error($"Can't validate migration verification user. Email: {model.Email}, is password empty - {string.IsNullOrEmpty(model.Password)}");
                return BadRequest();
            }

            var isMigrationRequired = true;
            bool passwordValidateStatus = false;
            try
            {
                passwordValidateStatus = _users.ValidateCredentials(model.Email, model.Password);
                Logger.Error("LoginValidation passwordValidateStatus - " + passwordValidateStatus);
                if (passwordValidateStatus)
                {
                    isMigrationRequired = !passwordValidateStatus;
                }
            }
            catch (InvalidHashException e)
            {
                Logger.Error(e);
                ModelState.AddModelError(string.Empty, AccountOptions.InvalidPasswordCheckMessage);
            }

            if (passwordValidateStatus)
            {
                return Ok(new
                {
                    TokenSuccess = passwordValidateStatus,
                    MigrationRequired = isMigrationRequired
                });
            }

            return BadRequest("Can't validate user password");
        }

        /// <summary>
        /// Entry point into the login workflow
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            // build a model so we know what to show on the login page
            var vm = await BuildLoginViewModelAsync(returnUrl);

            if (User.Identity is { IsAuthenticated: true })
            {
                var user = await _users.FindByEmailAsync(User.Identity.Name);
                if (!IsTosAccepted(user, returnUrl))
                {
                    return RedirectToAction("TermsAndConditions", "Account", new { returnUrl });
                }
            }

            if (vm.IsExternalLoginOnly)
            {
                // we only have one option for logging in and it's an external provider
                return RedirectToAction("Challenge", "External", new { provider = vm.ExternalLoginScheme, returnUrl });
            }

            return View("Login/Login.Default", vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel model, string button)
        {
            // check if we are in the context of an authorization request
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

            // the user clicked the "cancel" button
            if (button != _localizer["LogIn"].Value)
            {
                if (context != null)
                {
                    // if the user cancels, send a result back into IdentityServer as if they 
                    // denied the consent (even if this client does not require consent).
                    // this will send back an access denied OIDC error response to the client.
                    await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    if (context.IsNativeClient())
                    {
                        // The client is native, so this change in how to
                        // return the response is for better UX for the end user.
                        return this.LoadingPage("Redirect", model.ReturnUrl);
                    }

                    return Redirect(model.ReturnUrl);
                }

                // since we don't have a valid context, then we just go back to the home page
                return Redirect("~/");
            }

            if (!ModelState.IsValid) return await LoginPostView(model);

            // validate username/password against in-memory store
            bool passwordValidateStatus;
            try
            {
                passwordValidateStatus = _users.ValidateCredentials(model.Email, model.Password);
            }
            catch (InvalidHashException e)
            {
                Logger.Error(e);
                ModelState.AddModelError(string.Empty, AccountOptions.InvalidPasswordCheckMessage);
                var vm1 = await BuildLoginViewModelAsync(model);
                return View(vm1);
            }
            if (passwordValidateStatus)
            {
                var user = await _users.FindByEmailAsync(model.Email);
                await _events.RaiseAsync(new UserLoginSuccessEvent(user.Email, user.Id.ToString(), user.Email, clientId: context?.Client.ClientId));

                // only set explicit expiration here if user chooses "remember me". 
                // otherwise we rely upon expiration configured in cookie middleware.
                AuthenticationProperties props = null;
                if (AccountOptions.AllowRememberLogin && model.RememberLogin)
                {
                    props = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                    };
                }

                // issue authentication cookie with subject ID and username
                var isuser = new IdentityServerUser(user.Id.ToString())
                {
                    DisplayName = user.Email
                };

                await HttpContext.SignInAsync(isuser, props);

                if (context != null)
                {
                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    if (!context.IsNativeClient()) return Redirect(model.ReturnUrl);
                    if (!IsTosAccepted(user, model.ReturnUrl))
                    {
                        return RedirectToAction("TermsAndConditions", "Account", new { returnUrl = model.ReturnUrl });
                    }

                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    return Redirect(model.ReturnUrl);

                }

                if (!IsTosAccepted(user, model.ReturnUrl))
                {
                    return RedirectToAction("TermsAndConditions", "Account", new { returnUrl = model.ReturnUrl });
                }

                // request for a local page
                if (Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                if (string.IsNullOrEmpty(model.ReturnUrl))
                {
                    return Redirect("~/");
                }

                // user might have clicked on a malicious link - should be logged
                throw new ArgumentException("invalid return URL");
            }

            await _events.RaiseAsync(new UserLoginFailureEvent(model.Email, "invalid credentials", clientId: context?.Client.ClientId));
            ModelState.AddModelError(string.Empty, AccountOptions.InvalidCredentialsErrorMessage);

            // something went wrong, show form with error
            return await LoginPostView(model);
        }

        [AllowAnonymous]
        public ActionResult ForgotPassword(string clientId)
        {
            ViewBag.ClientIdToRedirect = clientId;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _users.FindByEmailAsync(model.Email);
                    var message = "EmailIsDisabled";
                    ModelState.AddModelError("", _localizer[message].Value);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex);
                return View("Error");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [AllowAnonymous]
        public async Task<ActionResult> ChangePassword(string userId, bool isPasswordExpaired)
        {
            // Logout if user still authorized.
            await SilentLogout();
            try
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    if (isPasswordExpaired)
                    {
                        AddErrors(IdentityResult.Failed(new IdentityError
                        {
                            Description = _localizer["YourPasswordHasExpired"].Value
                        }));
                    }

                    return View();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex);
            }

            return View("Error");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(UpdatePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _users.ChangePasswordAsync(model.UserId, model.CurrentPassword, model.Password);
            if (result.Succeeded)
            {
                var user = await _users.FindByIdAsync(model.UserId);
                if (user != null)
                {
                    return RedirectToAction("ChangePasswordConfirmation", "Account");
                }
            }

            AddErrors(result);

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> ChangePasswordFromApi([FromBody] ChangePasswordViewModel model)
        {
            IdentityResult result;
            if (model.UserId != User.GetUserId<int>())
            {
                var authorized = await _authorization.AuthorizeAsync(User, "Web:Admin");
                if (authorized.Succeeded)
                {
                    result = await _users.ChangePasswordByAdminAsync(model.UserId, model.Password);
                }
                else
                {
                    return Forbid();
                }
            }
            else
            {
                result = await _users.ChangePasswordAsync(model.UserId, model.CurrentPassword, model.Password);
            }

            if (result.Succeeded)
            {
                await _users.FindByIdAsync(model.UserId);
                return Ok("ok");
            }

            return Ok("error");

        }

        [AllowAnonymous]
        public ActionResult ChangePasswordConfirmation()
        {
            return View();
        }

        public async Task SendResetPasswordEmail(UserDetails user, string modelClientId)
        {
            try
            {
                var userOwnership = 1;
                string code = await _users.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code, clientId = modelClientId, ownerId = (int)userOwnership }, Request.Scheme);
                var resourceName = GetPasswordResetEmailResourceName(callbackUrl);
                string template;
                using (var reader = new StreamReader(resourceName))
                {
                    template = await reader.ReadToEndAsync();
                }

                template = template.Replace("@resetUrl", callbackUrl);
                template = template.Replace("@currentYear", DateTime.UtcNow.Year.ToString());
                var inline = GetInlinedLogo(callbackUrl);
                //send email
            }
            catch (Exception e)
            {
                Logger.Error(e);
                throw;
            }
        }

        [HttpPost]
        public async Task SendInvitationEmail([FromBody] User user)
        {
            try
            {
                var clientIdRecord = Request.Headers.FirstOrDefault(x => x.Key == "User-Auth-Client");
                var userOwnershipRecord = Request.Headers.FirstOrDefault(x => x.Key == "User-Ownership");
                string clientId = null;
                if (clientIdRecord.Key != null)
                {
                    clientId = clientIdRecord.Value;
                }


                string generatedCode = await _users.GeneratePasswordResetTokenAsync(user.Id);
                var userOwnership = 1;
                var callbackUrl = Url.Action("CreatePassword", "Account", new { userId = user.Id, code = generatedCode, clientAuthId = clientId, ownerId = (int)userOwnership }, Request.Scheme);

                var resourceName = GetInvitationEmailResourceName(callbackUrl);
                string template;
                using (var reader = new StreamReader(resourceName))
                {
                    template = await reader.ReadToEndAsync();
                }

                template = template.Replace("@resetUrl", callbackUrl);
                template = template.Replace("@userName", user.FirstName);
                template = template.Replace("@currentYear", DateTime.UtcNow.Year.ToString());

                LinkedResource inline = GetInlinedLogo(callbackUrl);
                // send email
            }
            catch (Exception e)
            {
                Logger.Error(e);
                throw;
            }
        }

        [AllowAnonymous]
        public ActionResult ResetPassword(string code, string clientId)
        {
            return code == null ? View("Error") : View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _users.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", _localizer["UnableFindUser"]);
                return View(model);
            }
            var result = await _users.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account", new { clientId = model.ClientId, ownerId = model.OwnerId });
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public async Task<ActionResult> CreatePassword(string userId, string code, string clientAuthId)
        {
            ViewBag.IsTokenExpired = false;
            if (string.IsNullOrEmpty(code))
            {
                return View("Error");
            }

            // Validate token 
            if (!await _users.VerifyUserTokenAsync(int.Parse(userId), code))
            {
                ViewBag.IsTokenExpired = true;
            }

            ViewBag.ClientAuthId = clientAuthId;

            return View();
        }

        //
        // POST: /Account/CreatePassword
        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreatePassword(CreatePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.IsTokenExpired = false;
                return View(model);
            }

            var userOwnership = 1;

            var user = await _users.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return StatusCode(404);
            }
            var result = await _users.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("CreatePasswordConfirmation", "Account", new { clientAuthId = model.ClientAuthId, ownerId = (int)userOwnership });
            }
            AddErrors(result);
            ViewBag.IsTokenExpired = true;
            return View();
        }

        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation(string clientId, int ownerId)
        {
            var userOwnership = 1;
            ViewBag.ClientAuthId = clientId;
            var allClients = Clients.Get();
            var selectedClient = allClients.FirstOrDefault(x => x.ClientId == clientId);
            if (selectedClient != null)
            {
                var ownerHost = "Default";
                var redirectUrl = selectedClient.RedirectUris.FirstOrDefault(x => x.Contains(ownerHost));
                if (redirectUrl == null)
                {
                    return Redirect(selectedClient.RedirectUris.First());
                }
                return Redirect(redirectUrl);
            }
            return View();
        }

        [AllowAnonymous]
        public ActionResult CreatePasswordConfirmation(string clientAuthId, int ownerId)
        {
            var userOwnership = 1;
            ViewBag.ClientAuthId = clientAuthId;
            var allClients = Clients.Get();
            var selectedClient = allClients.FirstOrDefault(x => x.ClientId == clientAuthId);
            if (selectedClient != null)
            {
                var ownerHost = "Default";
                var redirectUrl = selectedClient.RedirectUris.FirstOrDefault(x => x.Contains(ownerHost));
                if (redirectUrl == null)
                {
                    return Redirect(selectedClient.RedirectUris.First());
                }
                return Redirect(redirectUrl);
            }
            return View();
        }

        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        public async Task<IActionResult> TermsAndConditions(string returnUrl)
        {
            var user = await _users.FindByEmailAsync(User.Identity.Name);
            var term = new TermViewModel { Agree = false, UrlHash = returnUrl };
            return await TermsAndConditionsPostView(term);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TermsAndConditions(TermViewModel model)
        {
            if (model.Agree)
            {
                if (User.Identity is ClaimsIdentity)
                {
                    var claims = (User.Identity as ClaimsIdentity).Claims;
                    var subjectClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject);
                    if (subjectClaim != null)
                    {
                        var sourcePart = GetSourcePart(model.UrlHash);
                        //set terms accepted
                        var key = "cachedUserData_" + subjectClaim.Value;
                        _cacheService.ClearItem(key);
                        return View("Redirect", new RedirectViewModel { RedirectUrl = model.UrlHash });
                    }
                }

                return RedirectToAction("Login", "Account");
            }

            await SilentLogout();
            return RedirectToAction("Login", "Account", new { returnUrl = model.UrlHash });
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            // build a model so the logout page knows what to display
            var vm = await BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.
                return await Logout(vm);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

            if (User?.Identity.IsAuthenticated == true)
            {
                // delete local authentication cookie
                await HttpContext.SignOutAsync();

                // raise the logout event
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            }

            // check if we need to trigger sign-out at an upstream identity provider
            if (vm.TriggerExternalSignout)
            {
                // build a return URL so the upstream provider will redirect back
                // to us after the user has logged out. this allows us to then
                // complete our single sign-out processing.
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

                // this triggers a redirect to the external provider for sign-out
                return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
            }

            return View("LoggedOut", vm);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        /*****************************************/
        /* helper APIs for the AccountController */
        /*****************************************/

        private async Task SilentLogout()
        {
            await HttpContext.SignOutAsync();
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
            {
                var local = context.IdP == IdentityServerConstants.LocalIdentityProvider;

                // this is meant to short circuit the UI and only trigger the one external IdP
                var vm = new LoginViewModel
                {
                    EnableLocalLogin = local,
                    ReturnUrl = returnUrl,
                    Email = context.LoginHint
                };

                if (!local)
                {
                    vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };
                }

                return vm;
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => x.DisplayName != null
                )
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.DisplayName,
                    AuthenticationScheme = x.Name
                }).ToList();

            var allowLocal = true;
            if (context?.Client.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                    {
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }

            return new LoginViewModel
            {
                AllowRememberLogin = AccountOptions.AllowRememberLogin,
                EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
                ReturnUrl = returnUrl,
                Email = context?.LoginHint,
                ExternalProviders = providers.ToArray(),
                ClientId = context?.Client.ClientId
            };
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
        {
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
            vm.Email = model.Email;
            vm.RememberLogin = model.RememberLogin;
            return vm;
        }

        private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

            if (User?.Identity.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            return vm;
        }

        private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };

            if (User?.Identity.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != IdentityServerConstants.LocalIdentityProvider)
                {
                    var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout)
                    {
                        if (vm.LogoutId == null)
                        {
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                        }

                        vm.ExternalAuthenticationScheme = idp;
                    }
                }
            }

            return vm;
        }

        private async Task<IActionResult> TermsAndConditionsPostView(TermViewModel model)
        {
            var sourcePart = GetSourcePart(model.UrlHash);
            return View("TermsAndConditions/TermsAndConditions." + sourcePart, model);
        }

        private async Task<IActionResult> LoginPostView(LoginInputModel model)
        {
            var vm = await BuildLoginViewModelAsync(model);
            var sourcePart = GetSourcePart(model.ReturnUrl);
            return View("Login/Login." + sourcePart, vm);
        }

        private string GetSourcePart(string returnUrl)
        {
            string urlToRedirect;
            if (!string.IsNullOrEmpty(returnUrl))
            {
                urlToRedirect = HttpUtility.ParseQueryString(returnUrl).Get("redirect_uri");
            }
            else
            {
                urlToRedirect = null;
            }

            return "Default";
        }

        private bool IsTosAccepted(UserDetails user, string redirectUrl)
        {
            return true;
        }

        private string GetInvitationEmailResourceName(string callbackUrl)
        {
            var sourcePart = GetSourcePart(callbackUrl);
            var resourceName = _hostingEnvironment.WebRootPath + "/Content/Templates/UserInvitationTemplate." + sourcePart + ".html";
            return resourceName;
        }

        private string GetPasswordResetEmailResourceName(string callbackUrl)
        {
            var sourcePart = GetSourcePart(callbackUrl);
            var resourceName = _hostingEnvironment.WebRootPath + "/Content/Templates/ResetPasswordTemplate." + sourcePart + ".html";
            return resourceName;
        }

        private LinkedResource GetInlinedLogo(string callbackUrl)
        {
            var sourcePart = GetSourcePart(callbackUrl);
            LinkedResource inline = new LinkedResource(_environment.WebRootPath + @"\images\email-logo.png", "image/png");
            inline.ContentId = "logoImageId";
            return inline;
        }
    }
}