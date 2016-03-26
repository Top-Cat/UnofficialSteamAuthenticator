using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Models.SteamAuth
{
    internal class TimeQueryResponse : ModelBase
    {
        [JsonProperty("server_time")]
        public long ServerTime { get; set; }
    }
}
