using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Lib.Models.SteamAuth
{
    internal class LoginResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("login_complete")]
        public bool LoginComplete { get; set; }

        [JsonProperty("oauth")]
        public string OAuthDataString { get; set; }

        public OAuth OAuthData => this.OAuthDataString != null ? JsonConvert.DeserializeObject<OAuth>(this.OAuthDataString) : null;

        [JsonProperty("captcha_needed")]
        public bool CaptchaNeeded { get; set; }

        [JsonProperty("captcha_gid")]
        public string CaptchaGid { get; set; }

        [JsonProperty("emailsteamid")]
        public ulong EmailSteamId { get; set; }

        [JsonProperty("emailauth_needed")]
        public bool EmailAuthNeeded { get; set; }

        [JsonProperty("requires_twofactor")]
        public bool TwoFactorNeeded { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
