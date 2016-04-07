using System;
using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Lib.SteamAuth
{
    public interface ISteamSecrets
    {
        [JsonProperty("shared_secret")]
        string SharedSecret { get; set; }

        [JsonProperty("serial_number")]
        string SerialNumber { get; set; }

        [JsonProperty("revocation_code")]
        string RevocationCode { get; set; }

        [JsonProperty("uri")]
        string URI { get; set; }

        [JsonProperty("server_time")]
        long ServerTime { get; set; }

        [JsonProperty("account_name")]
        string AccountName { get; set; }

        [JsonProperty("token_gid")]
        string TokenGID { get; set; }

        [JsonProperty("identity_secret")]
        string IdentitySecret { get; set; }

        [JsonProperty("secret_1")]
        string Secret1 { get; set; }

        [JsonProperty("status")]
        int Status { get; set; }

        [JsonProperty("device_id")]
        string DeviceID { get; set; }

        [JsonProperty("personacache")]
        DateTime DisplayCache { get; }

        /// <summary>
        ///     Set to true if the authenticator has actually been applied to the account.
        /// </summary>
        [JsonProperty("fully_enrolled")]
        bool FullyEnrolled { get; set; }

        SessionData Session { get; set; }

        void PushStore();
        void GenerateSteamGuardCode(IWebRequest web, Callback makeRequest);
        void RefreshSession(SteamWeb web, BFCallback callback);
    }
}
