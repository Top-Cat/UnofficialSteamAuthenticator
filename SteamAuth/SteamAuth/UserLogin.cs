using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using UnofficialSteamAuthenticator.Lib.Models.SteamAuth;

namespace UnofficialSteamAuthenticator.Lib.SteamAuth
{
    /// <summary>
    ///     Handles logging the user into the mobile Steam website. Necessary to generate OAuth token and session cookies.
    /// </summary>
    public class UserLogin
    {
        private readonly CookieContainer _cookies = new CookieContainer();
        public string CaptchaGID;
        public string CaptchaText = null;
        public string EmailCode = null;
        public string EmailDomain = null;
        public bool LoggedIn;
        public string Password;

        public bool Requires2FA;

        public bool RequiresCaptcha;

        public bool RequiresEmail;

        public SessionData Session;
        public ulong SteamID;
        public string TwoFactorCode = null;
        public string Username;

        public UserLogin(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }

        public void DoLogin(SteamWeb web, LoginCallback callback)
        {
            var postData = new Dictionary<string, string>();
            CookieContainer cookies = this._cookies;

            WebCallback hasCookies = (res, code) =>
            {
                postData.Add("username", this.Username);
                web.MobileLoginRequest(ApiEndpoints.COMMUNITY_BASE + "/login/getrsakey", "POST", postData, cookies, (rsaRawResponse, rsaCode) =>
                {
                    if (rsaRawResponse == null || rsaCode != HttpStatusCode.OK || rsaRawResponse.Contains("<BODY>\nAn error occurred while processing your request."))
                    {
                        callback(LoginResult.GeneralFailure);
                        return;
                    }

                    var rsaResponse = JsonConvert.DeserializeObject<RSAResponse>(rsaRawResponse);

                    if (!rsaResponse.Success)
                    {
                        callback(LoginResult.BadRsa);
                        return;
                    }

                    var mod = new BigInteger(rsaResponse.Modulus, 16);
                    var exp = new BigInteger(rsaResponse.Exponent, 16);
                    var encryptEngine = new Pkcs1Encoding(new RsaEngine());
                    var rsaParams = new RsaKeyParameters(false, mod, exp);

                    encryptEngine.Init(true, rsaParams);

                    byte[] passwordArr = Encoding.UTF8.GetBytes(this.Password);
                    string encryptedPassword = Convert.ToBase64String(encryptEngine.ProcessBlock(passwordArr, 0, passwordArr.Length));

                    postData.Clear();
                    postData.Add("username", this.Username);
                    postData.Add("password", encryptedPassword);

                    postData.Add("twofactorcode", this.TwoFactorCode ?? "");

                    postData.Add("captchagid", this.RequiresCaptcha ? this.CaptchaGID : "-1");
                    postData.Add("captcha_text", this.RequiresCaptcha ? this.CaptchaText : "");

                    postData.Add("emailsteamid", this.Requires2FA || this.RequiresEmail ? this.SteamID.ToString() : "");
                    postData.Add("emailauth", this.RequiresEmail ? this.EmailCode : "");

                    postData.Add("rsatimestamp", rsaResponse.Timestamp);
                    postData.Add("remember_login", "false");
                    postData.Add("oauth_client_id", "DE45CD61");
                    postData.Add("oauth_scope", "read_profile write_profile read_client write_client");
                    postData.Add("loginfriendlyname", "#login_emailauth_friendlyname_mobile");
                    postData.Add("donotcache", Util.GetSystemUnixTime().ToString());

                    web.MobileLoginRequest(ApiEndpoints.COMMUNITY_BASE + "/login/dologin", "POST", postData, cookies, (rawLoginResponse, loginCode) =>
                    {
                        LoginResponse loginResponse = null;

                        if (rawLoginResponse != null && loginCode == HttpStatusCode.OK)
                        {
                            loginResponse = JsonConvert.DeserializeObject<LoginResponse>(rawLoginResponse);
                        }

                        if (loginResponse == null)
                        {
                            callback(LoginResult.GeneralFailure);
                            return;
                        }

                        if (loginResponse.Message != null && loginResponse.Message.Contains("Incorrect login"))
                        {
                            callback(LoginResult.BadCredentials);
                            return;
                        }

                        if (loginResponse.CaptchaNeeded)
                        {
                            this.RequiresCaptcha = true;
                            this.CaptchaGID = loginResponse.CaptchaGid;
                            callback(LoginResult.NeedCaptcha);
                            return;
                        }

                        if (loginResponse.EmailAuthNeeded)
                        {
                            this.RequiresEmail = true;
                            this.SteamID = loginResponse.EmailSteamId;
                            callback(LoginResult.NeedEmail);
                            return;
                        }

                        if (loginResponse.TwoFactorNeeded && !loginResponse.Success)
                        {
                            this.Requires2FA = true;
                            callback(LoginResult.Need2Fa);
                            return;
                        }

                        if (loginResponse.Message != null && loginResponse.Message.Contains("too many login failures"))
                        {
                            callback(LoginResult.TooManyFailedLogins);
                            return;
                        }

                        if (string.IsNullOrEmpty(loginResponse.OAuthData?.OAuthToken))
                        {
                            callback(LoginResult.GeneralFailure);
                            return;
                        }

                        if (!loginResponse.LoginComplete)
                        {
                            callback(LoginResult.BadCredentials);
                            return;
                        }

                        CookieCollection readableCookies = cookies.GetCookies(new Uri("https://steamcommunity.com"));
                        OAuth oAuthData = loginResponse.OAuthData;

                        this.Session = new SessionData
                        {
                            OAuthToken = oAuthData.OAuthToken,
                            SteamID = oAuthData.SteamId,
                            SteamLogin = oAuthData.SteamId + "%7C%7C" + oAuthData.SteamLogin,
                            SteamLoginSecure = oAuthData.SteamId + "%7C%7C" + oAuthData.SteamLoginSecure,
                            WebCookie = oAuthData.Webcookie,
                            SessionID = readableCookies["sessionid"].Value,
                            Username = this.Username
                        };
                        this.LoggedIn = true;
                        callback(LoginResult.LoginOkay);
                    });
                });
            };

            if (cookies.Count == 0)
            {
                //Generate a SessionID
                const string url = "https://steamcommunity.com/login?oauth_client_id=DE45CD61&oauth_scope=read_profile%20write_profile%20read_client%20write_client";
                cookies.Add(SteamWeb.uri, new Cookie("mobileClientVersion", "0 (2.1.3)", "/"));
                cookies.Add(SteamWeb.uri, new Cookie("mobileClient", "android", "/"));
                cookies.Add(SteamWeb.uri, new Cookie("Steam_Language", "english", "/"));

                var headers = new WebHeaderCollection
                {
                    ["X-Requested-With"] = "com.valvesoftware.android.steam.community"
                };

                web.MobileLoginRequest(url, "GET", null, cookies, headers, hasCookies);
            }
            else
            {
                hasCookies("", HttpStatusCode.OK);
            }
        }
    }

    public enum LoginResult
    {
        LoginOkay,
        GeneralFailure,
        BadRsa,
        BadCredentials,
        NeedCaptcha,
        Need2Fa,
        NeedEmail,
        TooManyFailedLogins
    }
}
