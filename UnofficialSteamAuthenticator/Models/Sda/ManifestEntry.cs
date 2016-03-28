using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Models.Sda
{
    public class ManifestEntry : ModelBase
    {
        [JsonProperty("encryption_iv")]
        public string Iv { get; set; }

        [JsonProperty("encryption_salt")]
        public string Salt { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("steamid")]
        public ulong SteamId { get; set; }
    }
}
