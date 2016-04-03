using System;
using System.Net;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using UnofficialSteamAuthenticator.Lib.SteamAuth;
using UnofficalSteamAuthenticator.Tests.Mock;

namespace UnofficalSteamAuthenticator.Tests.SteamAuth
{
    [TestClass]
    public class TimeAlignerTest
    {
        [TestMethod]
        public void TestAlignTime()
        {
            // This test method is dependent on callbacks being called syncronously when possible
            // Will need to be rewritten if this ever changes

            long testTime = Util.GetSystemUnixTime() + new Random().Next();

            var mock = new SteamWebMock();

            // Makes steamweb request, fails
            TimeAligner.AlignTime(mock, Assert.IsFalse);
            Assert.AreEqual(1, mock.CallCount("Request"));

            // No alignment yet, makes another failed request
            // Should still call the callback
            var called = false;
            TimeAligner.GetSteamTime(mock, time =>
            {
                called = true;
            });
            Assert.IsTrue(called);
            Assert.AreEqual(2, mock.CallCount("Request"));

            // Steamweb will now respond with a time
            mock.WithArgs("Request", ApiEndpoints.TWO_FACTOR_TIME_QUERY, "POST")(new object[] { "{response: {server_time: " + testTime + "}}", HttpStatusCode.OK });

            // No alignment yet, makes successful request
            // Should call the callback with an aligned time
            called = false;
            TimeAligner.GetSteamTime(mock, time =>
            {
                called = true;
                long diff = Math.Abs(time - testTime);
                Assert.IsTrue(diff <= 3);
                Assert.AreEqual(3, mock.CallCount("Request"));
            });
            Assert.IsTrue(called);

            // Has been aligned already, won't call request again
            // But should still call the callback with the current time
            // Assumes this test takes less than 3 seconds to run
            called = false;
            TimeAligner.GetSteamTime(mock, time =>
            {
                called = true;
                long diff = Math.Abs(time - testTime);
                Assert.IsTrue(diff <= 3);
                Assert.AreEqual(3, mock.CallCount("Request"));
            });
            Assert.IsTrue(called);
        }
    }
}
