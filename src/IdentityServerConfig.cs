using System;  
using System.Collections.Generic;
using System.Linq;  
using IdentityServer4;
using IdentityServer4.Models;

namespace SteamOpenIdConnectProvider
{
    public class IdentityServerConfig
    {
        public static IEnumerable<Client> GetClients(string clientId, string secret, string redirectUri, string logoutRedirectUri)
        {
            yield return new Client
            {
                ClientId = clientId,
                ClientName = "Proxy Client",
                AllowedGrantTypes = GrantTypes.Code,
                RequireConsent = false,
                ClientSecrets =
                {
                    new Secret(secret.Sha256())
                },
                AlwaysSendClientClaims = true,
                AlwaysIncludeUserClaimsInIdToken = true,

                // where to redirect to after login
                RedirectUris = redirectUri.Split(",").Select(x => x.Trim()).ToArray(),

                // where to redirect to after logout
                PostLogoutRedirectUris = { logoutRedirectUri },
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