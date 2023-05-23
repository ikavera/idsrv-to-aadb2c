using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using WebApi.Config;
using WebApi.Domain;
using WebApi.Mappers;
using WebApi.Mappers.Impl;

var builder = WebApplication.CreateBuilder(args);
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile($"appsettings.json", true, false)
    .AddJsonFile($"appsettings.{environment}.json", false, false);

ConfigHelper.LoadSettings(builder.Configuration);


builder.Services.AddScoped<IUserMapper, UserMapper>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Name = "oauth2",
        Type = SecuritySchemeType.OAuth2,
        Scheme = IdentityServerAuthenticationDefaults.AuthenticationScheme,
        In = ParameterLocation.Header,
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(ProjectSettings.AuthServer + "connect/authorize"),
                Scopes = new Dictionary<string, string> { { "api1", "Demo API - full access" } },
                TokenUrl = new Uri(ProjectSettings.AuthServer + "connect/token")
            }
        }
    });
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    c.OperationFilter<AuthorizeCheckOperationFilter>();
});

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

ProjectSettings.ValidIssuers.Add(ProjectSettings.AuthServer);
IdentityModelEventSource.ShowPII = true;

builder.Services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication("temp", options =>
{
    options.Authority = ProjectSettings.AuthServer;
    options.RequireHttpsMetadata = false; 
    options.TokenRetriever = CustomTokenRetriever.FromHeaderAndQueryString;
})
.AddJwtBearer(IdentityServerAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Authority = ProjectSettings.AuthServer;
    options.TokenValidationParameters.ValidIssuers = ProjectSettings.ValidIssuers;
    options.SecurityTokenValidators.Clear();
    options.SecurityTokenValidators.Add(new JwtSecurityTokenHandler
    {
        MapInboundClaims = false
    });
    options.TokenValidationParameters.NameClaimType = "name";
    options.TokenValidationParameters.RoleClaimType = "role";
    options.TokenValidationParameters.ValidateAudience = false;
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            if (ctx.Request.Method.Equals("GET") && ctx.Request.Query.ContainsKey("access_token"))
                ctx.Token = ctx.Request.Query["access_token"];
            return Task.CompletedTask;
        }
    };
});

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

builder.Services.AddMvc(options =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            });
ConfigHelper.AddPolicies(builder.Services);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    if (!ProjectSettings.IsSwaggerDebug)
    {
        string swaggerJsonBasePath = string.IsNullOrWhiteSpace(c.RoutePrefix) ? "." : "..";
        c.SwaggerEndpoint($"{swaggerJsonBasePath}/swagger/v1/swagger.json", "My API V1");
    }
    else
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty;
    }

    c.OAuthClientId("client.webapi.swagger");
    c.OAuthAppName("Demo API - Swagger");
});

var options = new RewriteOptions().AddRedirect("Swagger", "swagger");
app.UseRewriter(options);

app.UseCors("ApiCorsSettings");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
