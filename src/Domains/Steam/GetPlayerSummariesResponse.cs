using System.Collections.Generic;
using System.Text.Json.Serialization;
using SteamOpenIdConnectProvider.Profile.Models;

namespace SteamOpenIdConnectProvider.Domains.Steam
{
    public sealed class GetPlayerSummariesResponse
    {
        [JsonPropertyName("players")]
        public ICollection<Player> Players { get; set; }
    }
}