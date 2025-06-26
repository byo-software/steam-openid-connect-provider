namespace SteamOpenIdConnectProvider.Domains.Common;

public sealed class HostingConfig
{
    public const string Config = "Hosting";

    public string? BasePath { get; set; }

    public string? PublicOrigin { get; set; }
}
