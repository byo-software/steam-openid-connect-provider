using System;
using System.Collections.Generic;
using System.Linq;
using IdentityServer4;
using IdentityServer4.Models;
using SteamOpenIdConnectProvider.Domains.IdentityServer;

namespace SteamOpenIdConnectProvider.Models.IdentityServer
{
    public static class IdentityServerConfigFactory
    {
        public static IEnumerable<Client> GetClients(OpenIdConfig config)
        {
            yield return new Client
            {
                ClientId = config.ClientID,
                ClientName = config.ClientName,
                AllowedGrantTypes = GrantTypes.Code,
                RequireConsent = false,
                ClientSecrets =
                {
                    new Secret(config.ClientSecret.Sha256())
                },

                // where to redirect to after login
                RedirectUris = config.RedirectUri.Split(",").Select(x => x.Trim()).ToArray(),

                // where to redirect to after logout
                PostLogoutRedirectUris = { config.PostLogoutRedirectUri },
                RequirePkce = false,
                AllowedScopes = new List<string>
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                }
            };
        }

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile()
            };
        }
    }
}