using System.Collections.Generic;
using System.Net;
using UnofficialSteamAuthenticator.Lib.Models;
using UnofficialSteamAuthenticator.Lib.Models.SteamAuth;

namespace UnofficialSteamAuthenticator.Lib.SteamAuth
{
    public delegate void FinalizeCallback(AuthenticatorLinker.FinalizeResult result);

    public delegate void LinkCallback(AuthenticatorLinker.LinkResult response);

    public delegate void BCallback(bool response);

    public delegate void HasPhoneCallback(AuthenticatorLinker.PhoneStatus response);

    public delegate void FcCallback(List<Confirmation> response, WGTokenInvalidException ex);

    public delegate void SummaryCallback(Players players);

    public delegate void WebCallback(string response, HttpStatusCode statusCode);

    public delegate void Callback(string response);

    public delegate void LoginCallback(LoginResult result);

    public delegate void LongCallback(long time);

    internal delegate void ConfirmationCallback(ConfirmationDetailsResponse time);
}
