using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteamOpenIdConnectProvider.Domains.IdentityServer
{
    public class OpenIdConfig
    {
        public static readonly string Key = "OpenID";

        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
        public string PostLogoutRedirectUri { get; set; }
        public string ClientName { get; set; } = "Proxy Client";
    }
}
