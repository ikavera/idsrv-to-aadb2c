using log4net.Config;
using log4net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Models;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Reflection;
using WebApi.Config;
using WebApi.Domain;
using WebApiWithAad.Config;
using WebApiWithAad.Mappers.User;
using WebApiWithAad.Mappers.User.Impl;
using WebApiWithAad.Services.AzureGraph;
using WebApiWithAad.Services.Cache;
using WebApiWithAad.Services.User;
using WebApiWithAad.Services.UserGuid;

var builder = WebApplication.CreateBuilder(args);
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo($"log4net.{environment}.config"));

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile($"appsettings.json", true, false)
    .AddJsonFile($"appsettings.{environment}.json", false, false);

ConfigHelper.LoadSettings(builder.Configuration);

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<IUserMapper, UserMapper>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAzureGraphApiService, AzureGraphApiService>();
builder.Services.AddScoped<AuthHelper>();
builder.Services.AddScoped<IUserGuidToIdConverter, UserGuidToIdConverter>();


const string DefaultScheme = "Bearer";
AuthHelper authHelper = null;

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Name = "oauth2",
        Type = SecuritySchemeType.OAuth2,
        Scheme = DefaultScheme,
        In = ParameterLocation.Header,
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{builder.Configuration.GetValue<string>("AzureAdB2C:Instance")}{builder.Configuration.GetValue<string>("AzureAdB2C:Domain")}/{builder.Configuration.GetValue<string>("AzureAdB2C:SignUpSignInPolicyId")}/oauth2/v2.0/authorize"),
                Scopes = ProjectSettings.AadB2CAuthScopes,
                TokenUrl = new Uri($"{builder.Configuration.GetValue<string>("AzureAdB2C:Instance")}{builder.Configuration.GetValue<string>("AzureAdB2C:Domain")}/{builder.Configuration.GetValue<string>("AzureAdB2C:SignUpSignInPolicyId")}/oauth2/v2.0/token")
            }
        }
    });
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    c.OperationFilter<AuthorizeCheckOperationFilter>(); // Required to use access token
});

builder.Services.AddAuthentication(DefaultScheme)
                    .AddMicrosoftIdentityWebApi(options =>
                    {
                        builder.Configuration.Bind("AzureAdB2C", options);
                        options.TokenValidationParameters.NameClaimType = "name";
                        options.TokenValidationParameters.ValidateAudience = false;
                        options.Events = new JwtBearerEvents
                        {
                            OnTokenValidated = authHelper.SecurityTokenValidated,
                            OnMessageReceived = authHelper.MessageRecieved
                        };
                    },
                        options => { builder.Configuration.Bind("AzureAdB2C", options); })
                    .EnableTokenAcquisitionToCallDownstreamApi(options => builder.Configuration.Bind("AzureAdB2C", options))
                    .AddMicrosoftGraph(builder.Configuration.GetSection("GraphAPI"))
                    .AddInMemoryTokenCaches();

builder.Services.AddEndpointsApiExplorer();



ProjectSettings.ValidIssuers.Add(ProjectSettings.AuthServer);
IdentityModelEventSource.ShowPII = true;
ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;

if (ProjectSettings.CorsAllowedDomains.Any())
{
    builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiCorsSettings", builder =>
        builder.WithOrigins(ProjectSettings.CorsAllowedDomains.ToArray())
            .AllowAnyHeader()
            .WithExposedHeaders("Content-Disposition")
            .AllowAnyMethod());
});
}
else
{
    builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiCorsSettings", builder =>
        builder.AllowAnyOrigin()
            .AllowAnyHeader()
            .WithExposedHeaders("Content-Disposition")
            .AllowAnyMethod());
});
}

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

AuthHelper.AddPolicies(builder.Services);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        authHelper = scope.ServiceProvider.GetRequiredService<AuthHelper>();
    }
    catch (Exception ex)
    {
        Debug.WriteLine(ex);
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.OAuthClientId(builder.Configuration.GetValue<string>("AzureAdB2C:ClientId"));
    c.OAuthAppName("Me Demo API - Swagger");
});

app.UseHttpsRedirection();
app.UseResponseCaching();

app.UseCors("ApiCorsSettings");

app.UseMiddleware<EnableRequestBodyBufferingMiddleware>();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
