using System.Text.Json.Serialization;

namespace SteamOpenIdConnectProvider.Domains.Steam;

public sealed class SteamResponse<T>
{
    [JsonPropertyName("response")]
    public required T Response { get; set; }
}
