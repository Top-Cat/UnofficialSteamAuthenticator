using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Lib.Models.SteamAuth
{
    internal class ConfirmationDetailsResponse : SuccessResponse
    {
        [JsonProperty("html")]
        public string Html { get; set; }
    }
}
