using Newtonsoft.Json;

namespace SteamOpenIdConnectProvider.Profile.Models
{
    public sealed class SteamResponse<T>
    {
        [JsonProperty("response")]
        public T Response { get; set; }
    }
}