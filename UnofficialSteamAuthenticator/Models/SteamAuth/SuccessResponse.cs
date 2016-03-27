using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Models.SteamAuth
{
    internal class SuccessResponse : ModelBase
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
