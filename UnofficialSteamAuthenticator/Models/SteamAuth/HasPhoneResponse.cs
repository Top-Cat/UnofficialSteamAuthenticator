using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Models.SteamAuth
{
    internal class HasPhoneResponse : ModelBase
    {
        [JsonProperty("has_phone")]
        public bool HasPhone { get; set; }
    }
}
