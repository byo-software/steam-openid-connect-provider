namespace SteamOpenIdConnectProvider.Domains.Steam;

public sealed class SteamConfig
{
    public const string ConfigKey = "Steam";

    public required string ApplicationKey { get; set; }
}
