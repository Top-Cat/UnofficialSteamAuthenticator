using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Lib.Models
{
    public class Players : ModelBase
    {
        [JsonProperty("players")]
        public List<Player> PlayersList { get; set; }
    }
}
