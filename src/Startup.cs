using System;
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
using Microsoft.Extensions.Logging;
using Serilog;
using SteamOpenIdConnectProvider.Domains.Common;
using SteamOpenIdConnectProvider.Domains.IdentityServer;
using SteamOpenIdConnectProvider.Domains.Steam;
using SteamOpenIdConnectProvider.Models.IdentityServer;
using SteamOpenIdConnectProvider.Services;

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
            services.AddControllers();

            services.AddDbContext<AppInMemoryDbContext>(options =>
                options.UseInMemoryDatabase("default"));

            services.AddIdentity<IdentityUser, IdentityRole>(options =>
                {
                    options.User.AllowedUserNameCharacters = null;
                })
                .AddEntityFrameworkStores<AppInMemoryDbContext>()
                .AddDefaultTokenProviders();

            var openIdConfig = Configuration.GetSection(OpenIdConfig.Key);
            services
                .Configure<OpenIdConfig>(openIdConfig)
                .AddIdentityServer(options =>
                {
                    options.UserInteraction.LoginUrl = "/external-login";
                    options.UserInteraction.LogoutUrl = "/external-logout";
                })
                .AddAspNetIdentity<IdentityUser>()
                .AddInMemoryClients(IdentityServerConfigFactory.GetClients(openIdConfig.Get<OpenIdConfig>()))
                .AddInMemoryPersistedGrants()
                .AddDeveloperSigningCredential(true)
                .AddInMemoryIdentityResources(IdentityServerConfigFactory.GetIdentityResources());

            var steamConfig = Configuration.GetSection(SteamConfig.Key);
            services
                .Configure<SteamConfig>(steamConfig)
                .AddHttpClient<IProfileService, SteamProfileService>();

            services.AddAuthentication()
                .AddCookie(options =>
                {
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.Cookie.IsEssential = true;
                })
                .AddSteam(options =>
                {
                    options.ApplicationKey = steamConfig.Get<SteamConfig>().ApplicationKey;
                });

            services.AddHealthChecks()
                .AddUrlGroup(new Uri(Constants.OpenIdUrl), "Steam");
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var logger = app.ApplicationServices.GetRequiredService<ILogger<Startup>>();

            if (env.IsDevelopment())
            {
                logger.LogWarning("Starting up in development mode");
                app.UseDeveloperExceptionPage();
            }

            var hostingConfig = Configuration.GetSection(HostingConfig.Key).Get<HostingConfig>();
            if (!string.IsNullOrEmpty(hostingConfig.BasePath))
            {
                app.UsePathBase(hostingConfig.BasePath);
            }

            app.UseSerilogRequestLogging();

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
