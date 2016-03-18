using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Models
{
    internal class Players : ModelBase
    {
        [JsonProperty("players")]
        public List<Player> PlayersList { get; set; }
    }
}
