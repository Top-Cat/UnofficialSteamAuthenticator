using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using UnofficialSteamAuthenticator.Lib.Models;
using UnofficialSteamAuthenticator.Lib.Models.SteamAuth;

namespace UnofficialSteamAuthenticator.Lib.SteamAuth
{

    /// <summary>
    /// Handles the linking process for a new mobile authenticator.
    /// </summary>
    public class AuthenticatorLinker
    {
       

        /// <summary>
        /// Set to register a new phone number when linking. If a phone number is not set on the account, this must be set. If a phone number is set on the account, this must be null.
        /// </summary>
        public string PhoneNumber = null;

        public string Pin = null;

        /// <summary>
        /// Randomly-generated device ID. Should only be generated once per linker.
        /// </summary>
        public string DeviceID { get; private set; }

        /// <summary>
        /// After the initial link step, if successful, this will be the SteamGuard data for the account. PLEASE save this somewhere after generating it; it's vital data.
        /// </summary>
        public ISteamSecrets LinkedAccount { get; set; }

        /// <summary>
        /// True if the authenticator has been fully finalized.
        /// </summary>
        public bool Finalized = false;

        private readonly SessionData session;
        private readonly CookieContainer cookies;

        public AuthenticatorLinker(SessionData session)
        {
            this.session = session;
            this.DeviceID = Util.GenerateDeviceId();

            this.cookies = new CookieContainer();
            session.AddCookies(this.cookies);
        }

        public void AddAuthenticator(IWebRequest web, LinkCallback callback)
        {
            HasPhoneCallback hasPhoneCallback = hasPhone =>
            {
                if (hasPhone == PhoneStatus.FamilyView)
                {
                    callback(LinkResult.FamilyViewEnabled);
                    return;
                }
                bool settingNumber = !string.IsNullOrEmpty(this.PhoneNumber);
                if (hasPhone == PhoneStatus.Attached && settingNumber)
                {
                    callback(LinkResult.MustRemovePhoneNumber);
                    return;
                }
                if (hasPhone == PhoneStatus.Detatched && !settingNumber)
                {
                    callback(LinkResult.MustProvidePhoneNumber);
                    return;
                }

                BCallback numberAddedCallback = numberAdded =>
                {
                    if (!numberAdded)
                    {
                        callback(LinkResult.GeneralFailure);
                        return;
                    }

                    var postData = new Dictionary<string, string>();
                    postData["access_token"] = this.session.OAuthToken;
                    postData["steamid"] = this.session.SteamID.ToString();
                    postData["authenticator_type"] = "1";
                    postData["device_identifier"] = this.DeviceID;
                    postData["sms_phone_id"] = "1";

                    web.MobileLoginRequest(ApiEndpoints.STEAMAPI_BASE + "/ITwoFactorService/AddAuthenticator/v0001", "POST", postData, (response, code) =>
                    {
                        if (response == null || code != HttpStatusCode.OK)
                        {
                            callback(LinkResult.GeneralFailure);
                            return;
                        }

                        var addAuthenticatorResponse = JsonConvert.DeserializeObject<WebResponse<SteamGuardAccount>>(response);
                        if (addAuthenticatorResponse?.Response == null)
                        {
                            callback(LinkResult.GeneralFailure);
                            return;
                        }

                        if (addAuthenticatorResponse.Response.Status == 29)
                        {
                            callback(LinkResult.AuthenticatorPresent);
                            return;
                        }

                        if (addAuthenticatorResponse.Response.Status != 1)
                        {
                            callback(LinkResult.GeneralFailure);
                            return;
                        }

                        this.LinkedAccount = addAuthenticatorResponse.Response;
                        // Force not enrolled at this stage
                        this.LinkedAccount.FullyEnrolled = false;
                        this.LinkedAccount.Session = this.session;
                        this.LinkedAccount.DeviceID = this.DeviceID;

                        callback(LinkResult.AwaitingFinalization);
                    });
                };

                if (hasPhone == PhoneStatus.Detatched)
                {
                    this._addPhoneNumber(web, numberAddedCallback);
                } else
                {
                    numberAddedCallback(true);
                }
            };

            if (string.IsNullOrEmpty(this.Pin))
            {
                this._hasPhoneAttached(web, hasPhoneCallback);
            }
            else
            {
                this.UnlockFamilyView(web, success =>
                {
                    if (success)
                    {
                        this._hasPhoneAttached(web, hasPhoneCallback);
                    }
                    else
                    {
                        callback(LinkResult.FamilyViewEnabled);
                    }
                });
            }
        }

        public void FinalizeAddAuthenticator(IWebRequest web, string smsCode, FinalizeCallback callback)
        {
            var postData = new Dictionary<string, string>
            {
                ["steamid"] = this.session.SteamID.ToString(),
                ["access_token"] = this.session.OAuthToken,
                ["activation_code"] = smsCode
            };
            var tries = 0;

            Callback makeRequest = r => { };

            WebCallback reqCallback = (response, code) =>
            {
                if (tries > 30)
                {
                    callback(FinalizeResult.GeneralFailure);
                    return;
                }

                if (response == null || code != HttpStatusCode.OK)
                {
                    callback(FinalizeResult.GeneralFailure);
                    return;
                }

                var finalizeResponse = JsonConvert.DeserializeObject<WebResponse<FinalizeAuthenticatorResponse>>(response);

                if (finalizeResponse?.Response == null)
                {
                    callback(FinalizeResult.GeneralFailure);
                    return;
                }

                if (finalizeResponse.Response.Status == 89)
                {
                    callback(FinalizeResult.BadSMSCode);
                    return;
                }

                if (finalizeResponse.Response.Status == 88)
                {
                    if (tries >= 30)
                    {
                        callback(FinalizeResult.UnableToGenerateCorrectCodes);
                        return;
                    }
                }

                if (!finalizeResponse.Response.Success)
                {
                    callback(FinalizeResult.GeneralFailure);
                    return;
                }

                if (finalizeResponse.Response.WantMore)
                {
                    tries++;
                    this.LinkedAccount.GenerateSteamGuardCode(web, makeRequest);
                    return;
                }

                this.LinkedAccount.FullyEnrolled = true;
                callback(FinalizeResult.Success);
            };

            makeRequest = response => {
                postData["authenticator_code"] = response;
                TimeAligner.GetSteamTime(web, steamTime => {
                    postData["authenticator_time"] = steamTime.ToString();

                    web.MobileLoginRequest(ApiEndpoints.STEAMAPI_BASE + "/ITwoFactorService/FinalizeAddAuthenticator/v0001", "POST", postData, reqCallback);
                });
            };

            //The act of checking the SMS code is necessary for Steam to finalize adding the phone number to the account.
            //Of course, we only want to check it if we're adding a phone number in the first place...

            if (!string.IsNullOrEmpty(this.PhoneNumber))
            {
                this._checkSMSCode(web, smsCode, b =>
                {
                    if (b)
                    {
                        this.LinkedAccount.GenerateSteamGuardCode(web, makeRequest);
                    }
                    else
                    {
                        callback(FinalizeResult.BadSMSCode);
                    }
                });
            }
            else
            {
                this.LinkedAccount.GenerateSteamGuardCode(web, makeRequest);
            }
        }

        private void _checkSMSCode(IWebRequest web, string smsCode, BCallback callback)
        {
            var postData = new Dictionary<string, string>
            {
                ["op"] = "check_sms_code",
                ["arg"] = smsCode,
                ["sessionid"] = this.session.SessionID
            };

            web.Request(ApiEndpoints.COMMUNITY_BASE + "/steamguard/phoneajax", "POST", postData, this.cookies, (response, code) => {
                if (response == null || code != HttpStatusCode.OK)
                {
                    callback(false);
                    return;
                }

                var addPhoneNumberResponse = JsonConvert.DeserializeObject<SuccessResponse>(response);
                callback(addPhoneNumberResponse.Success);
            });
        }

        private void _addPhoneNumber(IWebRequest web, BCallback callback)
        {
            var postData = new Dictionary<string, string>
            {
                ["op"] = "add_phone_number",
                ["arg"] = this.PhoneNumber,
                ["sessionid"] = this.session.SessionID
            };

            web.Request(ApiEndpoints.COMMUNITY_BASE + "/steamguard/phoneajax", "POST", postData, this.cookies, (response, code) =>
            {
                if (response == null || code != HttpStatusCode.OK)
                {
                    callback(false);
                    return;
                }

                var addPhoneNumberResponse = JsonConvert.DeserializeObject<SuccessResponse>(response);
                callback(addPhoneNumberResponse.Success);
            });
        }

        private void _hasPhoneAttached(IWebRequest web, HasPhoneCallback callback)
        {
            var postData = new Dictionary<string, string>
            {
                ["op"] = "has_phone",
                ["arg"] = "null",
                ["sessionid"] = this.session.SessionID
            };

            web.Request(ApiEndpoints.COMMUNITY_BASE + "/steamguard/phoneajax", "POST", postData, this.cookies, (response, code) =>
            {
                if (code == HttpStatusCode.Forbidden)
                {
                    callback(PhoneStatus.FamilyView);
                    return;
                }

                if (response == null || code != HttpStatusCode.OK)
                {
                    callback(PhoneStatus.Detatched);
                    return;
                }

                var hasPhoneResponse = JsonConvert.DeserializeObject<HasPhoneResponse>(response);
                callback(hasPhoneResponse.HasPhone ? PhoneStatus.Attached : PhoneStatus.Detatched);
            });
        }

        private void UnlockFamilyView(IWebRequest web, BCallback callback)
        {
            var postData = new Dictionary<string, string>
            {
                ["pin"] = this.Pin
            };

            web.Request(ApiEndpoints.STORE_BASE + "/parental/ajaxunlock", "POST", postData, this.cookies, (response, code) =>
            {
                this.Pin = string.Empty; // Make sure we don't try again unless we fail

                if (response == null || code != HttpStatusCode.OK)
                {
                    callback(false);
                    return;
                }

                var successResponse = JsonConvert.DeserializeObject<SuccessResponse>(response);
                callback(successResponse.Success);
            });
        }

        public enum PhoneStatus
        {
            Attached,
            Detatched,
            FamilyView
        }

        public enum LinkResult
        {
            MustProvidePhoneNumber, //No phone number on the account
            MustRemovePhoneNumber, //A phone number is already on the account
            AwaitingFinalization, //Must provide an SMS code
            GeneralFailure, //General failure (really now!)
            AuthenticatorPresent,
            FamilyViewEnabled
        }

        public enum FinalizeResult
        {
            BadSMSCode,
            UnableToGenerateCorrectCodes,
            Success,
            GeneralFailure
        }
    }
}
