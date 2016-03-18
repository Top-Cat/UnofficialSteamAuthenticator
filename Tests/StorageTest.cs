using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Collections.Generic;
using Windows.Storage;
using UnofficialSteamAuthenticator;
using SteamAuth;

namespace Tests
{
    [TestClass]
    public class StorageTest
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private ulong testSteamid = 7656119800000000L;

        [TestInitialize]
        public void ClearLocalSettings()
        {
            localSettings.Values.Clear();
        }

        [TestMethod]
        public void TestLegacyBug()
        {
            localSettings.Values["steamUser-"] = testSteamid;
            localSettings.Values["steamGuard-" + testSteamid] = "{\"shared_secret\":\"##TEST##\",\"account_name\":\"testUser\",\"Session\":{\"SteamID\":" + testSteamid + "}}";
            SteamGuardAccount test = Storage.GetSteamGuardAccount("test");

            Assert.AreEqual("##TEST##", test.SharedSecret);
            Assert.AreEqual(localSettings.Values["steamUser-testUser"], testSteamid);
        }

        [TestMethod]
        public void TestOldAccounts()
        {
            localSettings.Values["sessionJson"] = "{\"SteamID\":" + testSteamid + "}";

            Dictionary<ulong, SessionData> accs = Storage.GetAccounts();
            Assert.AreEqual(1, accs.Count);

            SessionData acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, testSteamid);
        }

        [TestMethod]
        public void TestUpgradeAccounts()
        {
            SessionData acc;

            localSettings.Values["sessionJson"] = "{\"SteamID\":" + testSteamid + "}";
            acc = new SessionData();
            acc.SteamID = testSteamid + 1;

            Storage.PushStore(acc);

            Dictionary<ulong, SessionData> accs = Storage.GetAccounts();
            Assert.AreEqual(2, accs.Count);

            Storage.SetCurrentUser(testSteamid);
            acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, testSteamid);

            Storage.SetCurrentUser(testSteamid + 1);
            acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, testSteamid + 1);

            // Falls back to any value if unknown
            Storage.SetCurrentUser(testSteamid + 2);
            acc = Storage.GetSessionData();
            Assert.IsNotNull(acc);
        }

        [TestMethod]
        public void TestNewAccounts()
        {
            SessionData acc;

            localSettings.Values["sessionJson"] = "{\"" + testSteamid + "\":{\"SteamID\":" + testSteamid + "}}";

            Dictionary<ulong, SessionData> accs = Storage.GetAccounts();
            Assert.AreEqual(1, accs.Count);

            Storage.SetCurrentUser(testSteamid);
            acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, testSteamid);

            // Falls back to any value if unknown
            Storage.SetCurrentUser(testSteamid + 1);
            acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, testSteamid);
        }

        [TestMethod]
        public void TestSetUser()
        {
            Storage.SetCurrentUser(testSteamid);
            Assert.AreEqual(localSettings.Values["currentAccount"], testSteamid);
        }

        [TestMethod]
        public void TestLogout()
        {
            localSettings.Values["sessionJson"] = "{\"" + testSteamid + "\":{\"SteamID\":" + testSteamid + "}}";

            // Should do nothing as the account is not the current account
            Storage.Logout();
            Assert.AreEqual(1, Storage.GetAccounts().Count);

            Storage.SetCurrentUser(testSteamid);

            Storage.Logout();
            Assert.AreEqual(0, Storage.GetAccounts().Count);

            Assert.IsNull(localSettings.Values["currentAccount"]);
        }
    }
}
