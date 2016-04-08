using System;
using UnofficialSteamAuthenticator.Lib.SteamAuth;

namespace UnofficalSteamAuthenticator.Tests.Mock
{
    internal class SteamGuardAccountMock : Mock, ISteamSecrets
    {
        public string SharedSecret { get; set; }
        public string SerialNumber { get; set; }
        public string RevocationCode { get; set; }
        public string URI { get; set; }
        public long ServerTime { get; set; }
        public string AccountName { get; set; }
        public string TokenGID { get; set; }
        public string IdentitySecret { get; set; }
        public string Secret1 { get; set; }
        public int Status { get; set; }
        public string DeviceID { get; set; }
        public DateTime DisplayCache { get; set; }
        public bool FullyEnrolled { get; set; }
        public SessionData Session { get; set; }

        public void PushStore()
        {
            this.RegisterCall()();
        }

        public void GenerateSteamGuardCode(IWebRequest web, Callback makeRequest)
        {
            this.RegisterCall(web, makeRequest)();
            makeRequest((string) this.GetResponse(web)());
        }

        public void RefreshSession(SteamWeb web, BfCallback callback)
        {
            throw new NotImplementedException();
        }
    }
}
