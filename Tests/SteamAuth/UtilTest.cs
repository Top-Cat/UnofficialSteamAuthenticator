using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using UnofficialSteamAuthenticator.SteamAuth;

namespace UnofficalSteamAuthenticator.Tests.SteamAuth
{
    [TestClass]
    public class UtilTest
    {
        [TestMethod]
        public void TestTime()
        {
            // Very simple test
            Assert.IsTrue(Util.GetSystemUnixTime() < long.MaxValue);
            Assert.IsTrue(Util.GetSystemUnixTime() > long.MinValue);
            Assert.AreNotEqual(0, Util.GetSystemUnixTime());
        }
    }
}
