using System;
using System.Net;

namespace UnofficialSteamAuthenticator.Lib.SteamAuth
{
    public class SessionData
    {
        public string SessionID { get; set; }
        public string Username { get; set; }
        public string SteamLogin { get; set; }
        public string SteamLoginSecure { get; set; }
        public string WebCookie { get; set; }
        public string OAuthToken { get; set; }
        public ulong SteamID { get; set; }
        public long LastCurrent { get; set; }

        public void AddCookies(CookieContainer cookies)
        {
            Uri uri = SteamWeb.uri;
            cookies.Add(uri, new Cookie("mobileClientVersion", "0 (2.1.3)", "/"));
            cookies.Add(uri, new Cookie("mobileClient", "android", "/"));

            cookies.Add(uri, new Cookie("steamid", SteamID.ToString(), "/"));
            cookies.Add(uri, new Cookie("steamLogin", SteamLogin, "/")
            {
                HttpOnly = true
            });

            cookies.Add(uri, new Cookie("steamLoginSecure", SteamLoginSecure, "/")
            {
                HttpOnly = true,
                Secure = true
            });
            cookies.Add(uri, new Cookie("Steam_Language", "english", "/"));
            cookies.Add(uri, new Cookie("dob", "", "/"));
            cookies.Add(uri, new Cookie("sessionid", this.SessionID, "/"));
        }
    }
}
