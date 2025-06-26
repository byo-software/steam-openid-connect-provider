using System;
using System.Net.Http.Headers;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
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
        services
            .Configure<OpenIdConfig>(openIdConfig)
            .AddIdentityServer(options =>
            {
                options.UserInteraction.LoginUrl = "/external-login";
                options.UserInteraction.LogoutUrl = "/external-logout";
            })
            .AddAspNetIdentity<IdentityUser>()
            .AddProfileService<SteamProfileService>()
            .AddInMemoryClients(IdentityServerConfigFactory.GetClients(openIdConfig.Get<OpenIdConfig>()!))
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
}
