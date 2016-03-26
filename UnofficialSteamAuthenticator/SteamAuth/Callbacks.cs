namespace UnofficialSteamAuthenticator.SteamAuth
{
    public delegate void FinalizeCallback(AuthenticatorLinker.FinalizeResult result);
    public delegate void LinkCallback(AuthenticatorLinker.LinkResult response);
    public delegate void BCallback(bool response);
    public delegate void FCCallback(Confirmation[] response);
    public delegate void Callback(string response);
    public delegate void LoginCallback(LoginResult result);
    public delegate void LongCallback(long time);
}
