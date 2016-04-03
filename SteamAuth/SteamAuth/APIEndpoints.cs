namespace UnofficialSteamAuthenticator.Lib.SteamAuth
{
    public static class ApiEndpoints
    {
        public const string STEAMAPI_BASE = "https://api.steampowered.com";
        public const string COMMUNITY_BASE = "https://steamcommunity.com";
        public const string STORE_BASE = "https://steamcommunity.com";
        public const string MOBILEAUTH_BASE = STEAMAPI_BASE + "/IMobileAuthService/%s/v0001";
        public static string MOBILEAUTH_GETWGTOKEN = MOBILEAUTH_BASE.Replace("%s", "GetWGToken");
        public const string TWO_FACTOR_BASE = STEAMAPI_BASE + "/ITwoFactorService/%s/v0001";
        public static string TWO_FACTOR_TIME_QUERY = TWO_FACTOR_BASE.Replace("%s", "QueryTime");
        public const string USER_SUMMARIES_URL = STEAMAPI_BASE + "/ISteamUserOAuth/GetUserSummaries/v0001";
    }
}
