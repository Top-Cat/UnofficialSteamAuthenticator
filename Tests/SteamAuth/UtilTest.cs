using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using UnofficialSteamAuthenticator.Lib.SteamAuth;

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
            string deviceIdA = Util.GenerateDeviceId();
            string deviceIdB = Util.GenerateDeviceId();

            Assert.IsTrue(deviceIdA.Length > 0);
            Assert.IsFalse(deviceIdA.Equals(deviceIdB));
        }

        [TestMethod]
        public void TestConvertToSteam3()
        {
            Assert.AreEqual(46559209U, Util.ConvertToSteam3(76561198006824937));
            Assert.AreEqual(46559208U, Util.ConvertToSteam3(76561198006824936));
            Assert.AreEqual(1U, Util.ConvertToSteam3(76561197960265729));

            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                Util.ConvertToSteam3(76561197960265728);
            });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                Util.ConvertToSteam3(76561197960265720);
            });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                Util.ConvertToSteam3(76561266679742464);
            });
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
