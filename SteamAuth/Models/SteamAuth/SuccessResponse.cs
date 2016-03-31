using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Lib.Models.SteamAuth
{
    internal class SuccessResponse : ModelBase
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
