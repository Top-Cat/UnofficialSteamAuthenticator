using System.Collections.Generic;
using System.Net;

namespace UnofficialSteamAuthenticator.Lib.SteamAuth
{
    public interface IWebRequest
    {
        void MobileLoginRequest(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebHeaderCollection headers, Callback callback);
        void MobileLoginRequest(string url, string method, Dictionary<string, string> data, CookieContainer cookies, Callback callback);
        void MobileLoginRequest(string url, string method, Dictionary<string, string> data, Callback callback);
        void MobileLoginRequest(string url, string method, Callback callback);

        void Request(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebHeaderCollection headers, string referer, Callback callback);
        void Request(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebHeaderCollection headers, Callback callback);
        void Request(string url, string method, Dictionary<string, string> data, CookieContainer cookies, Callback callback);
        void Request(string url, string method, Dictionary<string, string> data, Callback callback);
        void Request(string url, string method, Callback callback);
    }
}
