using System.Collections.Generic;
using Windows.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using UnofficialSteamAuthenticator.Lib.SteamAuth;
using UnofficialSteamAuthenticator.Lib;

namespace UnofficialSteamAuthenticator.Tests
{
    [TestClass]
    public class StorageTest
    {
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private const ulong TestSteamid = 7656119800000000L;

        [TestInitialize]
        public void ClearLocalSettings()
        {
            this.localSettings.Values.Clear();
        }

        [TestMethod]
        public void TestLegacyBug()
        {
            this.localSettings.Values["steamUser-"] = TestSteamid;
            this.localSettings.Values["steamGuard-" + TestSteamid] = "{\"shared_secret\":\"##TEST##\",\"account_name\":\"testUser\",\"Session\":{\"SteamID\":" + TestSteamid + "}}";
            SteamGuardAccount test = Storage.GetSteamGuardAccount("test");

            Assert.AreEqual("##TEST##", test.SharedSecret);
            Assert.AreEqual(this.localSettings.Values["steamUser-testUser"], TestSteamid);
        }

        [TestMethod]
        public void TestOldAccounts()
        {
            this.localSettings.Values["sessionJson"] = "{\"SteamID\":" + TestSteamid + "}";

            Dictionary<ulong, SessionData> accs = Storage.GetAccounts();
            Assert.AreEqual(1, accs.Count);

            SessionData acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, TestSteamid);
        }

        [TestMethod]
        public void TestUpgradeAccounts()
        {
            this.localSettings.Values["sessionJson"] = "{\"SteamID\":" + TestSteamid + "}";
            var acc = new SessionData()
            {
                SteamID = TestSteamid + 1
            };

            Storage.PushStore(acc);

            Dictionary<ulong, SessionData> accs = Storage.GetAccounts();
            Assert.AreEqual(2, accs.Count);

            Storage.SetCurrentUser(TestSteamid);
            acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, TestSteamid);

            Storage.SetCurrentUser(TestSteamid + 1);
            acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, TestSteamid + 1);

            // Falls back to any value if unknown
            Storage.SetCurrentUser(TestSteamid + 2);
            acc = Storage.GetSessionData();
            Assert.IsNotNull(acc);
        }

        [TestMethod]
        public void TestNewAccounts()
        {
            SessionData acc;

            this.localSettings.Values["sessionJson"] = "{\"" + TestSteamid + "\":{\"SteamID\":" + TestSteamid + "}}";

            Dictionary<ulong, SessionData> accs = Storage.GetAccounts();
            Assert.AreEqual(1, accs.Count);

            Storage.SetCurrentUser(TestSteamid);
            acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, TestSteamid);

            // Falls back to any value if unknown
            Storage.SetCurrentUser(TestSteamid + 1);
            acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, TestSteamid);
        }

        [TestMethod]
        public void TestSetUser()
        {
            Storage.SetCurrentUser(TestSteamid);
            Assert.AreEqual(this.localSettings.Values["currentAccount"], TestSteamid);
        }

        [TestMethod]
        public void TestLogout()
        {
            this.localSettings.Values["sessionJson"] = "{\"" + TestSteamid + "\":{\"SteamID\":" + TestSteamid + "}}";

            // Should do nothing as the account is not the current account
            Storage.Logout();
            Assert.AreEqual(1, Storage.GetAccounts().Count);

            Storage.SetCurrentUser(TestSteamid);

            Storage.Logout();
            Assert.AreEqual(0, Storage.GetAccounts().Count);

            Assert.IsNull(this.localSettings.Values["currentAccount"]);
        }
    }
}
