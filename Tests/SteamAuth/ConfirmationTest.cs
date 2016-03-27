using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using UnofficialSteamAuthenticator.SteamAuth;

namespace UnofficalSteamAuthenticator.Tests.SteamAuth
{
    [TestClass]
    public class ConfirmationTest
    {
        [TestMethod]
        public void TestConfClassification()
        {
            var conf = new Confirmation();
            Assert.AreEqual(Confirmation.ConfirmationType.Unknown, conf.ConfType);

            conf = new Confirmation()
            {
                Description = ""
            };
            Assert.AreEqual(Confirmation.ConfirmationType.Unknown, conf.ConfType);

            conf = new Confirmation()
            {
                Description = "Confirm Test"
            };
            Assert.AreEqual(Confirmation.ConfirmationType.GenericConfirmation, conf.ConfType);

            conf = new Confirmation()
            {
                Description = "Trade with User"
            };
            Assert.AreEqual(Confirmation.ConfirmationType.Trade, conf.ConfType);

            conf = new Confirmation()
            {
                Description = "Sell - Item"
            };
            Assert.AreEqual(Confirmation.ConfirmationType.MarketSellTransaction, conf.ConfType);
        }
    }
}
