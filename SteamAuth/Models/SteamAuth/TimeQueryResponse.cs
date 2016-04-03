using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Lib.Models.SteamAuth
{
    internal class TimeQueryResponse : ModelBase
    {
        [JsonProperty("server_time")]
        public long ServerTime { get; set; }
    }
}
