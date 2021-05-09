using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SteamOpenIdConnectProvider.Profile.Models
{
    public sealed class GetPlayerSummariesResponse
    {
        [JsonPropertyName("players")]
        public ICollection<Player> Players { get; set; }
    }
}