using System.Globalization;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Auth.Web.UI.Config;
using Auth.Service.Cache;
using Auth.Service.Cache.Impl;
using Microsoft.IdentityModel.Logging;
using System.Net;

namespace Auth.Web.UI
{
    public class Startup
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Startup));

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Environment = environment;
            Configuration = configuration;
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo($"log4net.{Environment.EnvironmentName}.config"));
            Configuration = new ConfigurationBuilder()
                .SetBasePath(environment.ContentRootPath)
                .AddJsonFile($"appsettings.{Environment.EnvironmentName}.json")
                .Build();
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            ConfigHelper.LoadSettings(Configuration);
            var cert = ConfigHelper.LoadCertificate(Environment);
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, CacheService>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddRazorPages()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();
            services.AddScoped<IUserManager, UserManager>();

            IdentityModelEventSource.ShowPII = true;

            ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;

            services.ConfigureNonBreakingSameSiteCookies();

            services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }));

            services
                .AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                    options.EmitStaticAudienceClaim = true;
                })
                .AddProfileService<ProfileService>()
                .AddInMemoryClients(Clients.Get())
                .AddInMemoryIdentityResources(Config.Config.GetIdentityResources())
                .AddInMemoryApiScopes(Config.Config.GetScopes())
                .AddSigningCredential(cert)
                .AddExtensionGrantValidator<DelegationGrantValidator>()
                .AddExtensionGrantValidator<DesktopGrantValidator>()
                .AddResourceOwnerValidator<ResourceOwnerPasswordValidator>()
                .AddAuthorizeInteractionResponseGenerator<CustomInteractionResponseGenerator>();

            services.AddDataProtection()
                .SetApplicationName($"auth-server-{Environment.EnvironmentName}")
                .PersistKeysToFileSystem(new DirectoryInfo($@"{Environment.ContentRootPath}\keys"));

            services.AddLocalization(o => o.ResourcesPath = "Resources");
            var supportedCultures = new[]
            {
                new CultureInfo("en-US"),
                new CultureInfo("en-GB"),
                new CultureInfo("zh-HK"),
                new CultureInfo("zh-Hans"),
                new CultureInfo("zh"),
            };
            services.Configure<RequestLocalizationOptions>(options =>
                {

                    options.DefaultRequestCulture = new RequestCulture("en-US", "en-US");
                    options.SupportedCultures = supportedCultures;
                    options.SupportedUICultures = supportedCultures;
                });

            ConfigHelper.AddPolicies(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseCookiePolicy();
            app.UseRequestLocalization(app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>().Value);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                loggerFactory.AddLog4Net($"{env.ContentRootPath}\\log4net.{env.EnvironmentName}.config");
            }
            else
            {
                loggerFactory.AddLog4Net($"{env.ContentRootPath}\\log4net.{env.EnvironmentName}.config");
                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "text/html";
                        var exceptionHandlerPathFeature =
                            context.Features.Get<IExceptionHandlerPathFeature>();
                        Logger.Error(exceptionHandlerPathFeature.Error);
                    });
                });
                app.UseHsts();
            }

            app.UseStaticFiles();

            app.UseCors("MyPolicy");

            var fordwardedHeaderOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            fordwardedHeaderOptions.KnownNetworks.Clear();
            fordwardedHeaderOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(fordwardedHeaderOptions);

            app.UseIdentityServer();

            app.UseHttpsRedirection();
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add(
                    "Content-Security-Policy",
                    "style-src 'self' 'unsafe-inline'; " +
                    "script-src 'self' 'unsafe-inline'; " +
                    "img-src 'self' data:; ");

                await next();
            });

            app.UseRequestLocalization();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
