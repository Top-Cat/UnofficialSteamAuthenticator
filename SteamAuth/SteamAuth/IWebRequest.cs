using System.Collections.Generic;
using System.Net;

namespace UnofficialSteamAuthenticator.Lib.SteamAuth
{
    public interface IWebRequest
    {
        void MobileLoginRequest(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebHeaderCollection headers, WebCallback callback);
        void MobileLoginRequest(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebCallback callback);
        void MobileLoginRequest(string url, string method, Dictionary<string, string> data, WebCallback callback);
        void MobileLoginRequest(string url, string method, WebCallback callback);

        void Request(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebHeaderCollection headers, string referer, WebCallback callback);
        void Request(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebHeaderCollection headers, WebCallback callback);
        void Request(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebCallback callback);
        void Request(string url, string method, Dictionary<string, string> data, WebCallback callback);
        void Request(string url, string method, WebCallback callback);
    }
}
