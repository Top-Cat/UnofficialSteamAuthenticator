using System.Collections.Generic;
using Windows.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SteamAuth;
using UnofficialSteamAuthenticator;

namespace UnofficalSteamAuthenticator.Tests
{
    [TestClass]
    public class StorageTest
    {
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private readonly ulong testSteamid = 7656119800000000L;

        [TestInitialize]
        public void ClearLocalSettings()
        {
            this.localSettings.Values.Clear();
        }

        [TestMethod]
        public void TestLegacyBug()
        {
            this.localSettings.Values["steamUser-"] = this.testSteamid;
            this.localSettings.Values["steamGuard-" + this.testSteamid] = "{\"shared_secret\":\"##TEST##\",\"account_name\":\"testUser\",\"Session\":{\"SteamID\":" + this.testSteamid + "}}";
            SteamGuardAccount test = Storage.GetSteamGuardAccount("test");

            Assert.AreEqual("##TEST##", test.SharedSecret);
            Assert.AreEqual(this.localSettings.Values["steamUser-testUser"], this.testSteamid);
        }

        [TestMethod]
        public void TestOldAccounts()
        {
            this.localSettings.Values["sessionJson"] = "{\"SteamID\":" + this.testSteamid + "}";

            Dictionary<ulong, SessionData> accs = Storage.GetAccounts();
            Assert.AreEqual(1, accs.Count);

            SessionData acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, this.testSteamid);
        }

        [TestMethod]
        public void TestUpgradeAccounts()
        {
            SessionData acc;

            this.localSettings.Values["sessionJson"] = "{\"SteamID\":" + this.testSteamid + "}";
            acc = new SessionData();
            acc.SteamID = this.testSteamid + 1;

            Storage.PushStore(acc);

            Dictionary<ulong, SessionData> accs = Storage.GetAccounts();
            Assert.AreEqual(2, accs.Count);

            Storage.SetCurrentUser(this.testSteamid);
            acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, this.testSteamid);

            Storage.SetCurrentUser(this.testSteamid + 1);
            acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, this.testSteamid + 1);

            // Falls back to any value if unknown
            Storage.SetCurrentUser(this.testSteamid + 2);
            acc = Storage.GetSessionData();
            Assert.IsNotNull(acc);
        }

        [TestMethod]
        public void TestNewAccounts()
        {
            SessionData acc;

            this.localSettings.Values["sessionJson"] = "{\"" + this.testSteamid + "\":{\"SteamID\":" + this.testSteamid + "}}";

            Dictionary<ulong, SessionData> accs = Storage.GetAccounts();
            Assert.AreEqual(1, accs.Count);

            Storage.SetCurrentUser(this.testSteamid);
            acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, this.testSteamid);

            // Falls back to any value if unknown
            Storage.SetCurrentUser(this.testSteamid + 1);
            acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, this.testSteamid);
        }

        [TestMethod]
        public void TestSetUser()
        {
            Storage.SetCurrentUser(this.testSteamid);
            Assert.AreEqual(this.localSettings.Values["currentAccount"], this.testSteamid);
        }

        [TestMethod]
        public void TestLogout()
        {
            this.localSettings.Values["sessionJson"] = "{\"" + this.testSteamid + "\":{\"SteamID\":" + this.testSteamid + "}}";

            // Should do nothing as the account is not the current account
            Storage.Logout();
            Assert.AreEqual(1, Storage.GetAccounts().Count);

            Storage.SetCurrentUser(this.testSteamid);

            Storage.Logout();
            Assert.AreEqual(0, Storage.GetAccounts().Count);

            Assert.IsNull(this.localSettings.Values["currentAccount"]);
        }
    }
}
