using System.Text.Json.Serialization;

namespace SteamOpenIdConnectProvider.Profile.Models
{
    public sealed class SteamResponse<T>
    {
        [JsonPropertyName("response")]
        public T Response { get; set; }
    }
}