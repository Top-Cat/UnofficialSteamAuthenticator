using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Models.SteamAuth
{
    internal class RSAResponse : SuccessResponse
    {
        [JsonProperty("publickey_exp")]
        public string Exponent { get; set; }

        [JsonProperty("publickey_mod")]
        public string Modulus { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("steamid")]
        public ulong SteamID { get; set; }
    }
}
