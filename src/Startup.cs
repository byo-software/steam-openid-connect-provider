using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SteamOpenIdConnectProvider.Domains.Common;
using SteamOpenIdConnectProvider.Domains.IdentityServer;
using SteamOpenIdConnectProvider.Domains.Steam;

namespace SteamOpenIdConnectProvider;

public sealed class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddDbContext<AppInMemoryDbContext>(options =>
            options.UseInMemoryDatabase("default"));

        services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.User.AllowedUserNameCharacters = string.Empty;
                options.User.RequireUniqueEmail = false;
                options.Lockout.AllowedForNewUsers = false;
            })
            .AddEntityFrameworkStores<AppInMemoryDbContext>()
            .AddDefaultTokenProviders();

        var openIdConfig = configuration.GetSection(OpenIdConfig.ConfigKey);
        var openIdSettings = openIdConfig.Get<OpenIdConfig>()!;
        services
            .Configure<OpenIdConfig>(openIdConfig)
            .AddIdentityServer(options =>
            {
                options.UserInteraction.LoginUrl = "/external-login";
                options.UserInteraction.LogoutUrl = "/external-logout";
            })
            .AddAspNetIdentity<IdentityUser>()
            .AddProfileService<SteamProfileService>()
            .AddInMemoryClients(IdentityServerConfigFactory.GetClients(openIdSettings))
            .AddInMemoryPersistedGrants()
            .AddDeveloperSigningCredential()
            .AddInMemoryIdentityResources(IdentityServerConfigFactory.GetIdentityResources());

        var steamConfig = configuration.GetSection(SteamConfig.ConfigKey);
        services
            .Configure<SteamConfig>(steamConfig)
            .AddHttpClient<IProfileService, SteamProfileService>();

        services.AddAuthentication()
            .AddCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            })
            .AddSteam(options =>
            {
                options.ApplicationKey = steamConfig.Get<SteamConfig>()!.ApplicationKey;

                if (openIdSettings.ReturnErrorsToClient)
                {
                    options.Events.OnRemoteFailure = context =>
                        HandleSteamRemoteFailure(context, options.StateDataFormat);
                }
            });
            
        services.Configure<CookiePolicyOptions>(options =>
        {
            options.Secure = CookieSecurePolicy.Always;
            options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
            options.OnAppendCookie = cookieContext =>
                SetSameSiteCookieOption(cookieContext.Context, cookieContext.CookieOptions);
            options.OnDeleteCookie = cookieContext =>
                SetSameSiteCookieOption(cookieContext.Context, cookieContext.CookieOptions);
        });

        services.AddHealthChecks()
            .AddUrlGroup(
                uri: new Uri(SteamConstants.OpenIdUrl), 
                name: "Steam",
                configureClient: (_, client) =>
                {
                    var userAgentHeaders
                        = client.DefaultRequestHeaders.UserAgent;

                    userAgentHeaders.Clear();
                    userAgentHeaders.Add(new ProductInfoHeaderValue("SteamOpenIdConnectProvider", "1.1.0"));
                });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var hostingConfig = configuration.GetSection(HostingConfig.Config).Get<HostingConfig>()!;
        var forwardOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            RequireHeaderSymmetry = false
        };

        forwardOptions.KnownNetworks.Clear();
        forwardOptions.KnownProxies.Clear();

        app.UseForwardedHeaders(forwardOptions);

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCookiePolicy(new CookiePolicyOptions
        {
            Secure = CookieSecurePolicy.Always,
            MinimumSameSitePolicy = SameSiteMode.Unspecified,
            OnAppendCookie = cookieContext =>
                SetSameSiteCookieOption(cookieContext.Context, cookieContext.CookieOptions),
            OnDeleteCookie = cookieContext =>
                SetSameSiteCookieOption(cookieContext.Context, cookieContext.CookieOptions)
        });

        app.UseAuthentication();

        app.Use(async (ctx, next) =>
        {
            if (!string.IsNullOrWhiteSpace(hostingConfig.PublicOrigin))
            {
                ctx.SetIdentityServerOrigin(hostingConfig.PublicOrigin);
            }

            if (!string.IsNullOrWhiteSpace(hostingConfig.BasePath))
            {
                ctx.SetIdentityServerBasePath(hostingConfig.BasePath);
            }

            await next();
        });

        app.UseSerilogRequestLogging();
        app.UseRouting();
        app.UseIdentityServer();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health");
        });
    }

    private static void SetSameSiteCookieOption(HttpContext httpContext, CookieOptions options)
    {
        if (options.SameSite != SameSiteMode.None)
        {
            return;
        }

        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        if (userAgent.Contains("CPU iPhone OS 12")
            || userAgent.Contains("iPad; CPU OS 12")
            || (userAgent.Contains("Macintosh; Intel Mac OS X 10_14")
                && userAgent.Contains("Version/")
                && userAgent.Contains("Safari"))
            || userAgent.Contains("Chrome/5")
            || userAgent.Contains("Chrome/6"))
        {
            options.SameSite = SameSiteMode.Unspecified;
        }
    }

    // When OpenId:ReturnErrorsToClient is enabled, turn a failed Steam login into a standard
    // OAuth2 error response back to the originating client instead of surfacing an HTTP 500.
    private static async Task HandleSteamRemoteFailure(
        RemoteFailureContext context,
        ISecureDataFormat<AuthenticationProperties>? stateFormat)
    {
        // On a correlation/timeout failure the framework leaves context.Properties null, so recover
        // the authentication properties by unprotecting the state carried on the callback request.
        // Properties.RedirectUri is the external login callback, whose returnUrl query parameter
        // carries the original OIDC authorize request.
        var properties = context.Properties;
        if (properties is null && stateFormat is not null)
        {
            var state = context.Request.Query["state"].ToString();
            properties = string.IsNullOrEmpty(state) ? null : stateFormat.Unprotect(state);
        }

        var returnUrl = GetReturnUrl(properties?.RedirectUri);
        if (returnUrl == null)
        {
            // Cannot recover the original request; let the failure propagate (unchanged behaviour).
            return;
        }

        var interaction = context.HttpContext.RequestServices
            .GetRequiredService<IIdentityServerInteractionService>();
        var request = await interaction.GetAuthorizationContextAsync(returnUrl);
        if (request is null || request.RedirectUri is null)
        {
            // Not a valid authorization request; let the failure propagate (unchanged behaviour).
            return;
        }

        Log.Warning(context.Failure, "Steam login failed; returning an error response to the client.");

        var errorUrl = QueryHelpers.AddQueryString(request.RedirectUri, new Dictionary<string, string?>
        {
            ["error"] = "temporarily_unavailable",
            ["error_description"] = "The Steam login could not be completed. Please try again.",
            ["state"] = request.Parameters["state"],
        });

        context.HandleResponse();
        context.Response.Redirect(errorUrl);
    }

    private static string? GetReturnUrl(string? callbackUri)
    {
        if (string.IsNullOrEmpty(callbackUri))
        {
            return null;
        }

        var queryIndex = callbackUri.IndexOf('?');
        if (queryIndex < 0)
        {
            return null;
        }

        var query = QueryHelpers.ParseQuery(callbackUri[(queryIndex + 1)..]);
        return query.TryGetValue("returnUrl", out var returnUrl) ? returnUrl.ToString() : null;
    }
}
