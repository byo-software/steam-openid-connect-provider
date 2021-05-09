using System.Text.Json.Serialization;

namespace SteamOpenIdConnectProvider.Models.Steam
{
    public sealed class SteamResponse<T>
    {
        [JsonPropertyName("response")]
        public T Response { get; set; }
    }
}