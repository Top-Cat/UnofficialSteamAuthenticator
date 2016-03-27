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

        [TestMethod]
        public void TestDeviceId()
        {
            string deviceIdA = Util.GenerateDeviceID();
            string deviceIdB = Util.GenerateDeviceID();

            Assert.IsTrue(deviceIdA.Length > 0);
            Assert.IsFalse(deviceIdA.Equals(deviceIdB));
        }

        [TestMethod]
        public void TestSplitRatios()
        {
            var result = "";

            result = Util.SplitOnRatios("abcdefghijklmnopqrstuvwxyz", new[] { 2, 3, 4, 4 }, "-");
            Assert.AreEqual("ab-cde-fghi-jklm", result);

            result = Util.SplitOnRatios("abcdefghijklmnopqrstuvwxyz", new[] { 2, 3, 4, 4 }, "___");
            Assert.AreEqual("ab___cde___fghi___jklm", result);

            result = Util.SplitOnRatios("abc", new[] { 2, 3, 4, 4 }, "-");
            Assert.AreEqual("ab-c", result);

            result = Util.SplitOnRatios("abcdefghijklmnopqrstuvwxyz", new[] { 2 }, "-");
            Assert.AreEqual("ab", result);

            result = Util.SplitOnRatios("abcdefghijklmnopqrstuvwxyz", new int[] { }, "-");
            Assert.AreEqual("", result);

            result = Util.SplitOnRatios("abcdefghijklmnopqrstuvwxyz", new[] { 2, 3, 4, 4 }, "");
            Assert.AreEqual("abcdefghijklm", result);
        }
    }
}
