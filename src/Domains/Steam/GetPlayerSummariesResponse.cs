using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SteamOpenIdConnectProvider.Domains.Steam;

public sealed class GetPlayerSummariesResponse
{
    [JsonPropertyName("players")]
    public required ICollection<Player> Players { get; set; }
}
