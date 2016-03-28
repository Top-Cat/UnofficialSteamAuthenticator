using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Models.Sda
{
    public class Manifest : ModelBase
    {
        [JsonProperty("encrypted")]
        public bool Encrypted { get; set; }

        [JsonProperty("entries")]
        public List<ManifestEntry> Entries { get; set; }
    }
}
