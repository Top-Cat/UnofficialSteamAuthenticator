using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Models.SteamAuth
{
    internal class OAuth : ModelBase
    {
        [JsonProperty("steamid")]
        public ulong SteamId { get; set; }

        [JsonProperty("oauth_token")]
        public string OAuthToken { get; set; }

        [JsonProperty("wgtoken")]
        public string SteamLogin { get; set; }

        [JsonProperty("wgtoken_secure")]
        public string SteamLoginSecure { get; set; }

        [JsonProperty("webcookie")]
        public string Webcookie { get; set; }
    }
}
