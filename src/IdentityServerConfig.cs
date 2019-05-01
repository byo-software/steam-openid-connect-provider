using System.Collections.Generic;
using IdentityServer4;
using IdentityServer4.Models;

namespace SteamOpenIdConnectProxy
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

                ClientSecrets =
                {
                    new Secret(secret.Sha256())
                },

                // where to redirect to after login
                RedirectUris = { redirectUri },

                // where to redirect to after logout
                PostLogoutRedirectUris = { logoutRedirectUri },

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
                new IdentityResources.Profile(),
            };
        }
    }
}