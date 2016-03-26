using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace UnofficialSteamAuthenticator.SteamAuth
{

    public class SteamWeb : IWebRequest
    {
        public static Uri uri = new Uri("https://steamcommunity.com");

        /// <summary>
        /// Perform a mobile login request
        /// </summary>
        /// <param name="url">API url</param>
        /// <param name="method">GET or POST</param>
        /// <param name="data">Name-data pairs</param>
        /// <param name="cookies">current cookie container</param>
        /// <returns>response body</returns>
        public void MobileLoginRequest(Callback callback, string url, string method, Dictionary<string, string> data = null, CookieContainer cookies = null, WebHeaderCollection headers = null)
        {
            Request(url, method, data, cookies, headers, APIEndpoints.COMMUNITY_BASE + "/mobilelogin?oauth_client_id=DE45CD61&oauth_scope=read_profile%20write_profile%20read_client%20write_client", callback);
        }

        public void Request(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebHeaderCollection headers, string referer, Callback callback)
        {
            List<string> dataFormatted = new List<string>();
            if (data != null)
            {
                foreach (KeyValuePair<string, string> entry in data)
                {
                    dataFormatted.Add(String.Format("{0}={1}", WebUtility.UrlEncode(entry.Key), WebUtility.UrlEncode(entry.Value)));
                }
            }

            string query = string.Join("&", dataFormatted);

            if (method == "GET")
            {
                url += (url.Contains("?") ? "&" : "?") + query;
            }

            if (headers == null)
            {
                headers = new WebHeaderCollection();
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
            foreach (string key in headers.AllKeys)
            {
                request.Headers[key] = headers[key];
            }
            request.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Linux; U; Android 4.1.1; en-us; Google Nexus 4 - 4.1.1 - API 16 - 768x1280 Build/JRO03S) AppleWebKit/534.30 (KHTML, like Gecko) Version/4.0 Mobile Safari/534.30";
            request.Headers[HttpRequestHeader.Referer] = referer;

            if (cookies != null)
            {
                request.CookieContainer = cookies;
            }

            if (method == "POST")
            {
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Headers[HttpRequestHeader.ContentLength] = query.Length.ToString();

                request.BeginGetRequestStream(iar =>
                {
                    HttpWebRequest req = (HttpWebRequest)iar.AsyncState;
                    byte[] byteArray = Encoding.UTF8.GetBytes(query);

                    using (Stream postStream = req.EndGetRequestStream(iar))
                    {
                        postStream.Write(byteArray, 0, query.Length);
                    }

                    req.BeginGetResponse(fin =>
                    {
                        ResponseCallback(fin, callback);
                    }, req);
                }, request);
            } else {
                request.BeginGetResponse(fin =>
                {
                    ResponseCallback(fin, callback);
                }, request);
            }
        }

        public void Request(string url, string method, Dictionary<string, string> data, CookieContainer cookies, WebHeaderCollection headers, Callback callback)
        {
            Request(url, method, data, cookies, headers, APIEndpoints.COMMUNITY_BASE, callback);
        }

        public void Request(string url, string method, Dictionary<string, string> data, CookieContainer cookies, Callback callback)
        {
            Request(url, method, data, cookies, null, callback);
        }

        public void Request(string url, string method, Dictionary<string, string> data, Callback callback)
        {
            Request(url, method, data, null, callback);
        }

        public void Request(string url, string method, Callback callback)
        {
            Request(url, method, null, callback);
        }

        private static void ResponseCallback(IAsyncResult result, Callback callback)
        {
            HttpWebRequest request = (HttpWebRequest)result.AsyncState;
            try {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(result))
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        callback(null);
                    }

                    using (StreamReader responseStream = new StreamReader(response.GetResponseStream()))
                    {
                        callback(responseStream.ReadToEnd());
                    }
                }
            }
            catch (WebException)
            {
                callback(null);
            }
        }
    }
}
