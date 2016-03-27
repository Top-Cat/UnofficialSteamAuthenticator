using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using UnofficalSteamAuthenticator.Tests.Mock;
using UnofficialSteamAuthenticator.SteamAuth;

namespace UnofficalSteamAuthenticator.Tests.SteamAuth
{
    [TestClass]
    public class AddAuthenticatorTest
    {
        private readonly SessionData sessionData;
        private AuthenticatorLinker linker;
        private const string TestPhone = "test-phone";

        private readonly Func<Dictionary<string, string>, bool> checkHasPhone;
        private readonly Func<Dictionary<string, string>, bool> checkAddPhone;
        private readonly Func<Dictionary<string, string>, bool> checkAddAuthenticator;

        public AddAuthenticatorTest()
        {
            this.sessionData = new SessionData()
            {
                Username = "test-username",
                SteamID = 7656119800000000L,
                OAuthToken = "test-oauth",
                SessionID = "test-sessionid",
                SteamLogin = "test-steamlogin",
                SteamLoginSecure = "test-steamsecure",
                WebCookie = "test-webcookie"
            };

            this.checkHasPhone = postData =>
                postData["op"].Equals("has_phone") &&
                postData["sessionid"].Equals("test-sessionid");

            this.checkAddPhone = postData =>
                postData["op"].Equals("add_phone_number") &&
                postData["arg"].Equals(TestPhone) &&
                postData["sessionid"].Equals("test-sessionid");

            this.checkAddAuthenticator = postData =>
                postData["access_token"].Equals(this.sessionData.OAuthToken) &&
                postData["steamid"].Equals(this.sessionData.SteamID.ToString()) &&
                postData["authenticator_type"].Equals("1") &&
                postData["device_identifier"].Length > 0 &&
                postData["sms_phone_id"].Equals("1");
        }

        [TestInitialize]
        public void Init()
        {
            this.linker = new AuthenticatorLinker(this.sessionData);
        }

        [TestMethod]
        public void TestHasPhone()
        {
            var mock = new SteamWebMock();

            // Use matcher instead of passing dictionary directly because .Equals won't work
            mock.WithArgs("Request", APIEndpoints.COMMUNITY_BASE + "/steamguard/phoneajax", "POST", this.checkHasPhone)("{has_phone: false}");

            this.linker.AddAuthenticator(mock, response =>
            {
                Assert.AreEqual(AuthenticatorLinker.LinkResult.MustProvidePhoneNumber, response);
            });
        }

        [TestMethod]
        public void TestRemovePhone()
        {
            this.linker.PhoneNumber = TestPhone;

            var mock = new SteamWebMock();
            mock.WithArgs("Request", APIEndpoints.COMMUNITY_BASE + "/steamguard/phoneajax", "POST", this.checkHasPhone)("{has_phone: true}");

            this.linker.AddAuthenticator(mock, response =>
            {
                Assert.AreEqual(AuthenticatorLinker.LinkResult.MustRemovePhoneNumber, response);
            });
        }

        [TestMethod]
        public void TestAddPhone()
        {
            this.linker.PhoneNumber = TestPhone;

            var mock = new SteamWebMock();
            mock.WithArgs("Request", APIEndpoints.COMMUNITY_BASE + "/steamguard/phoneajax", "POST", this.checkHasPhone)("{has_phone: false}");

            this.linker.AddAuthenticator(mock, response =>
            {
                Assert.AreEqual(AuthenticatorLinker.LinkResult.GeneralFailure, response);
            });

            // Should try and add phone
            Assert.AreEqual(1, mock.CallCount("Request", APIEndpoints.COMMUNITY_BASE + "/steamguard/phoneajax", "POST", this.checkAddPhone));
        }

        [TestMethod]
        public void TestAddAuthenticator()
        {
            var mock = new SteamWebMock();
            mock.WithArgs("Request", APIEndpoints.COMMUNITY_BASE + "/steamguard/phoneajax", "POST", this.checkHasPhone)("{has_phone: true}");

            this.linker.AddAuthenticator(mock, response =>
            {
                Assert.AreEqual(AuthenticatorLinker.LinkResult.GeneralFailure, response);
            });

            // Should try and add phone
            Assert.AreEqual(1, mock.CallCount("Request", APIEndpoints.STEAMAPI_BASE + "/ITwoFactorService/AddAuthenticator/v0001", "POST", this.checkAddAuthenticator));

            var mockB = (SteamWebMock) mock.Clone();
            mock.WithArgs("Request", APIEndpoints.STEAMAPI_BASE + "/ITwoFactorService/AddAuthenticator/v0001", "POST", this.checkAddAuthenticator)("{}");

            this.linker.AddAuthenticator(mock, response =>
            {
                Assert.AreEqual(AuthenticatorLinker.LinkResult.GeneralFailure, response);
            });

            mockB.WithArgs("Request", APIEndpoints.STEAMAPI_BASE + "/ITwoFactorService/AddAuthenticator/v0001", "POST", this.checkAddAuthenticator)("{response: {status: 0}}");

            this.linker.AddAuthenticator(mockB, response =>
            {
                Assert.AreEqual(AuthenticatorLinker.LinkResult.GeneralFailure, response);
            });
        }

        [TestMethod]
        public void TestAuthenticatorPresent()
        {
            var mock = new SteamWebMock();
            mock.WithArgs("Request", APIEndpoints.COMMUNITY_BASE + "/steamguard/phoneajax", "POST", this.checkHasPhone)("{has_phone: true}");
            mock.WithArgs("Request", APIEndpoints.STEAMAPI_BASE + "/ITwoFactorService/AddAuthenticator/v0001", "POST", this.checkAddAuthenticator)("{response: {status: 29}}");

            this.linker.AddAuthenticator(mock, response =>
            {
                Assert.AreEqual(AuthenticatorLinker.LinkResult.AuthenticatorPresent, response);
            });
        }

        [TestMethod]
        public void TestAwaitingFinalization()
        {
            long testTime = Util.GetSystemUnixTime();

            var mock = new SteamWebMock();
            mock.WithArgs("Request", APIEndpoints.COMMUNITY_BASE + "/steamguard/phoneajax", "POST", this.checkHasPhone)("{has_phone: true}");

            mock.WithArgs("Request", APIEndpoints.STEAMAPI_BASE + "/ITwoFactorService/AddAuthenticator/v0001", "POST", this.checkAddAuthenticator)
                ("{response: {status: 1, shared_secret: \"test-sharedsecret\", serial_number: \"test-serialnum\", revocation_code: \"test-revcode\", uri: \"test-uri\", server_time: " + testTime + ", " +
                 "account_name: \"test-accountname\", token_gid: \"test-tokengid\", identity_secret: \"test-identsecret\", secret_1: \"test-secret1\", " +
                 "device_id: \"test-deviceid\", fully_enrolled: true}}"); // Rogue values, should be ignored

            this.linker.AddAuthenticator(mock, response =>
            {
                Assert.AreEqual(AuthenticatorLinker.LinkResult.AwaitingFinalization, response);
                Assert.AreEqual(this.sessionData, this.linker.LinkedAccount.Session);
                Assert.IsTrue(this.linker.LinkedAccount.DeviceID.Length > 0);

                // I assume this is used for restoring from storage and is never part of the response
                Assert.AreNotEqual("test-deviceid", this.linker.LinkedAccount.DeviceID);
                Assert.IsFalse(this.linker.LinkedAccount.FullyEnrolled);

                Assert.AreEqual("test-sharedsecret", this.linker.LinkedAccount.SharedSecret);
                Assert.AreEqual("test-serialnum", this.linker.LinkedAccount.SerialNumber);
                Assert.AreEqual("test-revcode", this.linker.LinkedAccount.RevocationCode);
                Assert.AreEqual("test-uri", this.linker.LinkedAccount.URI);
                Assert.AreEqual("test-accountname", this.linker.LinkedAccount.AccountName);
                Assert.AreEqual("test-tokengid", this.linker.LinkedAccount.TokenGID);
                Assert.AreEqual("test-identsecret", this.linker.LinkedAccount.IdentitySecret);
                Assert.AreEqual("test-secret1", this.linker.LinkedAccount.Secret1);

                Assert.AreEqual(testTime, this.linker.LinkedAccount.ServerTime);
                Assert.AreEqual(1, this.linker.LinkedAccount.Status);
            });
        }
    }
}
