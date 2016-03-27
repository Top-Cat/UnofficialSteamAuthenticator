using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Models.SteamAuth
{
    internal class FinalizeAuthenticatorResponse : SuccessResponse
    {
        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("server_time")]
        public long ServerTime { get; set; }

        [JsonProperty("want_more")]
        public bool WantMore { get; set; }
    }
}
