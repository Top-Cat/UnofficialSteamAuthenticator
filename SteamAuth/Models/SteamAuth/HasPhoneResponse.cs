using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Lib.Models.SteamAuth
{
    internal class HasPhoneResponse : ModelBase
    {
        [JsonProperty("has_phone")]
        public bool HasPhone { get; set; }
    }
}
