using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Collections.Generic;
using Windows.Storage;
using UnofficialSteamAuthenticator;
using SteamAuth;

namespace UnofficalSteamAuthenticator.Tests
{
    [TestClass]
    public class StorageTest
    {
        readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private readonly ulong testSteamid = 7656119800000000L;

        [TestInitialize]
        public void ClearLocalSettings()
        {
            _localSettings.Values.Clear();
        }

        [TestMethod]
        public void TestLegacyBug()
        {
            _localSettings.Values["steamUser-"] = testSteamid;
            _localSettings.Values["steamGuard-" + testSteamid] = "{\"shared_secret\":\"##TEST##\",\"account_name\":\"testUser\",\"Session\":{\"SteamID\":" + testSteamid + "}}";
            SteamGuardAccount test = Storage.GetSteamGuardAccount("test");

            Assert.AreEqual("##TEST##", test.SharedSecret);
            Assert.AreEqual(_localSettings.Values["steamUser-testUser"], testSteamid);
        }

        [TestMethod]
        public void TestOldAccounts()
        {
            _localSettings.Values["sessionJson"] = "{\"SteamID\":" + testSteamid + "}";

            Dictionary<ulong, SessionData> accs = Storage.GetAccounts();
            Assert.AreEqual(1, accs.Count);

            SessionData acc = Storage.GetSessionData();
            Assert.AreEqual(acc.SteamID, testSteamid);
        }

        [TestMethod]
        public void TestUpgradeAccounts()
        {
            SessionData acc;

            _localSettings.Values["sessionJson"] = "{\"SteamID\":" + testSteamid + "}";
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

            _localSettings.Values["sessionJson"] = "{\"" + testSteamid + "\":{\"SteamID\":" + testSteamid + "}}";

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
            Assert.AreEqual(_localSettings.Values["currentAccount"], testSteamid);
        }

        [TestMethod]
        public void TestLogout()
        {
            _localSettings.Values["sessionJson"] = "{\"" + testSteamid + "\":{\"SteamID\":" + testSteamid + "}}";

            // Should do nothing as the account is not the current account
            Storage.Logout();
            Assert.AreEqual(1, Storage.GetAccounts().Count);

            Storage.SetCurrentUser(testSteamid);

            Storage.Logout();
            Assert.AreEqual(0, Storage.GetAccounts().Count);

            Assert.IsNull(_localSettings.Values["currentAccount"]);
        }
    }
}
