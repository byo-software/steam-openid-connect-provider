using System;
using System.Net.Http;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IdentityServer4.Services;
using Microsoft.AspNetCore.HttpOverrides;
using SteamOpenIdConnectProvider.Services;
using SteamOpenIdConnectProvider.Models.IdentityServer;
using SteamOpenIdConnectProvider.Domains.Common;
using SteamOpenIdConnectProvider.Domains.IdentityServer;
using SteamOpenIdConnectProvider.Domains.Steam;

namespace SteamOpenIdConnectProvider
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddDbContext<AppInMemoryDbContext>(options =>
                options.UseInMemoryDatabase("default"));

            services.AddIdentity<IdentityUser, IdentityRole>(options =>
                {
                    options.User.AllowedUserNameCharacters = null;
                })
                .AddEntityFrameworkStores<AppInMemoryDbContext>()
                .AddDefaultTokenProviders();

            var openIdConfig = Configuration.GetSection(OpenIdConfig.Key).Get<OpenIdConfig>();
            services.AddIdentityServer(options =>
                {
                    options.UserInteraction.LoginUrl = "/ExternalLogin";
                })
                .AddAspNetIdentity<IdentityUser>()
                .AddInMemoryClients(IdentityServerConfigFactory.GetClients(openIdConfig))
                .AddInMemoryPersistedGrants()
                .AddDeveloperSigningCredential(true)
                .AddInMemoryIdentityResources(IdentityServerConfigFactory.GetIdentityResources());

            var steamConfig = Configuration.GetSection(SteamConfig.Key).Get<SteamConfig>();
            services
                .Configure<SteamConfig>(Configuration.GetSection(SteamConfig.Key))
                .AddHttpClient<IProfileService, SteamProfileService>();

            services.AddAuthentication()
                .AddCookie(options =>
                {
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.Cookie.IsEssential = true;
                })
                .AddSteam(options =>
                {
                    options.ApplicationKey = steamConfig.ApplicationKey;
                });

            services.AddHealthChecks()
                .AddUrlGroup(new Uri("https://steamcommunity.com/openid"), "Steam");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var hostingConfig = Configuration.GetSection(HostingConfig.Key).Get<HostingConfig>();
            if (!string.IsNullOrEmpty(hostingConfig.BasePath))
            {
                app.UsePathBase(hostingConfig.BasePath);
            }

            app.UseCookiePolicy();
            app.Use(async (ctx, next) =>
            {
                if (!string.IsNullOrEmpty(hostingConfig.PublicOrigin))
                {
                    ctx.SetIdentityServerOrigin(hostingConfig.PublicOrigin);
                }

                await next();
            });

            var forwardOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                RequireHeaderSymmetry = false
            };

            forwardOptions.KnownNetworks.Clear();
            forwardOptions.KnownProxies.Clear();

            app.UseForwardedHeaders(forwardOptions);
            app.UseRouting();
            app.UseIdentityServer();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }
    }
}
