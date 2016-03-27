using System;
using System.Collections.Generic;
using System.Net;
using UnofficialSteamAuthenticator.SteamAuth;

namespace UnofficalSteamAuthenticator.Tests.Mock
{
    public class SteamWebMock : Mock, IWebRequest
    {
        public SteamWebMock() { }
        public SteamWebMock(List<Tuple<string, List<object>>> calls, Dictionary<string, Dictionary<List<object>, object>> responses) : base(calls, responses) { }

        public void MobileLoginRequest(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebHeaderCollection headers, Callback callback)
        {
            const string referer = APIEndpoints.COMMUNITY_BASE + "/mobilelogin?oauth_client_id=DE45CD61&oauth_scope=read_profile%20write_profile%20read_client%20write_client";

            this.RegisterCall(url, method, data, cookies, headers, referer, callback)();
            callback((string) this.GetResponse(url, method, data, cookies, headers, referer)());
        }

        public void MobileLoginRequest(string url, string method, Dictionary<string, string> data, CookieContainer cookies, Callback callback)
        {
            this.MobileLoginRequest(url, method, data, cookies, null, callback);
        }

        public void MobileLoginRequest(string url, string method, Dictionary<string, string> data, Callback callback)
        {
            this.MobileLoginRequest(url, method, data, null, callback);
        }

        public void MobileLoginRequest(string url, string method, Callback callback)
        {
            this.MobileLoginRequest(url, method, null, callback);
        }

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
