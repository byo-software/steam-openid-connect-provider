using System.Collections.Generic;
using Newtonsoft.Json;

namespace SteamOpenIdConnectProvider.Profile.Models
{
    public sealed class GetPlayerSummariesResponse
    {
        [JsonProperty("players")]
        public ICollection<Player> Players { get; set; }
    }
}