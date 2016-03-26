using System.Collections.Generic;
using System.Net;
using UnofficialSteamAuthenticator.SteamAuth;

namespace UnofficalSteamAuthenticator.Tests.Mock
{
    public class SteamWebMock : Mock, IWebRequest
    {
        public void Request(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebHeaderCollection headers, string referer, Callback callback)
        {
            this.RegisterCall(url, method, data, cookies, headers, referer, callback)();
            callback((string) this.GetResponse(url, method, data, cookies, headers, referer)());
        }

        public void Request(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebHeaderCollection headers, Callback callback)
        {
            this.Request(url, method, data, cookies, headers, APIEndpoints.COMMUNITY_BASE, callback);
        }

        public void Request(string url, string method, Dictionary<string, string> data, CookieContainer cookies, Callback callback)
        {
            this.Request(url, method, data, cookies, null, callback);
        }

        public void Request(string url, string method, Dictionary<string, string> data, Callback callback)
        {
            this.Request(url, method, data, null, callback);
        }

        public void Request(string url, string method, Callback callback)
        {
            this.Request(url, method, null, callback);
        }
    }
}
