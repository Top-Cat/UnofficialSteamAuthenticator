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
    public class FinalizeAddAuthenticatorTest
    {
        private readonly SessionData sessionData;
        private AuthenticatorLinker linker;
        private const string TestSmsCode = "##SMSCODE##";

        private readonly Func<Dictionary<string, string>, bool> checkSmsCode;

        public FinalizeAddAuthenticatorTest()
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

            this.checkSmsCode = postData =>
                postData["op"].Equals("check_sms_code") &&
                postData["arg"].Equals(TestSmsCode) &&
                postData["sessionid"].Equals("test-sessionid");
        }

        [TestInitialize]
        public void Init()
        {
            this.linker = new AuthenticatorLinker(this.sessionData);
        }

        [TestMethod]
        public void TestHasPhone()
        {
            this.linker.PhoneNumber = "test-phone";

            var mock = new SteamWebMock();

            // Use matcher instead of passing dictionary directly because .Equals won't work
            mock.WithArgs("Request", APIEndpoints.COMMUNITY_BASE + "/steamguard/phoneajax", "POST", this.checkSmsCode)("{success: false}");

            this.linker.FinalizeAddAuthenticator(mock, TestSmsCode, response =>
            {
                Assert.AreEqual(AuthenticatorLinker.FinalizeResult.BadSMSCode, response);
            });
        }
    }
}
