using System;
using System.Collections.Generic;

namespace SteamOpenIdConnectProvider.Domains.IdentityServer;

public sealed class OpenIdConfig
{
    public const string ConfigKey = "OpenId";

    public required string ClientId { get; set; }
    
    public required string ClientSecret { get; set; }
    
    public string? RedirectUri { get; set; }
    
    public string? PostLogoutRedirectUri { get; set; }
    
    public string ClientName { get; set; } = "Proxy Client";

    public IEnumerable<string> RedirectUris => (RedirectUri ?? string.Empty).Split(
        [',', ';'],
        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    public IEnumerable<string> PostLogoutRedirectUris => (PostLogoutRedirectUri ?? string.Empty).Split(
        [',', ';'],
        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
