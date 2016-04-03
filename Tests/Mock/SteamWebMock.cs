using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnofficialSteamAuthenticator.Lib.SteamAuth;

namespace UnofficalSteamAuthenticator.Tests.Mock
{
    public class SteamWebMock : Mock, IWebRequest
    {
        public SteamWebMock() { }
        public SteamWebMock(List<Tuple<string, List<object>>> calls, Dictionary<string, Dictionary<List<object>, object>> responses) : base(calls, responses) { }

        public void MobileLoginRequest(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebHeaderCollection headers, WebCallback callback)
        {
            const string referer = APIEndpoints.COMMUNITY_BASE + "/mobilelogin?oauth_client_id=DE45CD61&oauth_scope=read_profile%20write_profile%20read_client%20write_client";

            this.RegisterCall(url, method, data, cookies, headers, referer, callback)();
            object[] response = (object[]) this.GetResponse(url, method, data, cookies, headers, referer)() ?? new object[] { null, HttpStatusCode.NotFound };
            callback((string) response[0], (HttpStatusCode) response[1]);
        }

        public void MobileLoginRequest(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebCallback callback)
        {
            this.MobileLoginRequest(url, method, data, cookies, null, callback);
        }

        public void MobileLoginRequest(string url, string method, Dictionary<string, string> data, WebCallback callback)
        {
            this.MobileLoginRequest(url, method, data, null, callback);
        }

        public void MobileLoginRequest(string url, string method, WebCallback callback)
        {
            this.MobileLoginRequest(url, method, null, callback);
        }

        public void Request(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebHeaderCollection headers, string referer, WebCallback callback)
        {
            this.RegisterCall(url, method, data, cookies, headers, referer, callback)();

            object[] response = (object[]) this.GetResponse(url, method, data, cookies, headers, referer)() ?? new object[] { null, HttpStatusCode.NotFound };
            callback((string) response[0], (HttpStatusCode) response[1]);
        }

        public void Request(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebHeaderCollection headers, WebCallback callback)
        {
            this.Request(url, method, data, cookies, headers, APIEndpoints.COMMUNITY_BASE, callback);
        }

        public void Request(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebCallback callback)
        {
            this.Request(url, method, data, cookies, null, callback);
        }

        public void Request(string url, string method, Dictionary<string, string> data, WebCallback callback)
        {
            this.Request(url, method, data, null, callback);
        }

        public void Request(string url, string method, WebCallback callback)
        {
            this.Request(url, method, null, callback);
        }
    }
}
