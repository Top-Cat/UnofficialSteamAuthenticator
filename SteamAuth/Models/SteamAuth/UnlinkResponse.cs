using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Lib.Models.SteamAuth
{
    internal class UnlinkResponse : SuccessResponse
    {
        [JsonProperty("revocation_attempts_remaining")]
        public int AttemptsRemaining { get; set; }
    }
}
