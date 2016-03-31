using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using UnofficialSteamAuthenticator;
using UnofficialSteamAuthenticator.Lib;

namespace UnofficalSteamAuthenticator.Tests
{
    [TestClass]
    public class FileEncryptorTest
    {
        [TestMethod]
        public void TestIv()
        {
            IBuffer iv = CryptographicBuffer.DecodeFromBase64String(FileEncryptor.GetInitializationVector());
            Assert.AreEqual(16U, iv.Length);
        }

        [TestMethod]
        public void TestSalt()
        {
            IBuffer iv = CryptographicBuffer.DecodeFromBase64String(FileEncryptor.GetRandomSalt());
            Assert.AreEqual(8U, iv.Length);
        }

        [TestMethod]
        public void TestGetKey()
        {
            IBuffer iBuff = FileEncryptor.GetEncryptionKey("MPZtsFKrUjxT7CJBQdMHeL55", "d+WTC5HTMiI=");
            string buff = Convert.ToBase64String(iBuff.ToArray());

            Assert.AreEqual("zxtE+siw7hDl7bhbpSC1WjWmlQU9YwyZVTLj2JxBGro=", buff);
        }

        [TestMethod]
        public void TestDecrypt()
        {
            string response = FileEncryptor.DecryptData(
                "MPZtsFKrUjxT7CJBQdMHeL55",
                "d+WTC5HTMiI=",
                "LMbOhhdhBAz81yYlE8eAfQ==",
                "czIgppp/gijz+WtaHGTeh4ryjd1dOS7Pn6pJQa3VXZRnDL4DCdD68PBjLBjgZ7Li/LOhi20GwjFi6Y1YI5O6Zeaw6VzpH2BZAUtyBcsjZscIn93o99kPKG2ULnQJ2mRWWylAk7KsRUJAeDEqPG18wA=="
            );

            Assert.AreEqual("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec fringilla eu lacus sed ullamcorper.", response);
        }

        [TestMethod]
        public void TestEncrypt()
        {
            string response = FileEncryptor.EncryptData(
                "MPZtsFKrUjxT7CJBQdMHeL55",
                "d+WTC5HTMiI=",
                "LMbOhhdhBAz81yYlE8eAfQ==",
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec fringilla eu lacus sed ullamcorper."
            );

            Assert.AreEqual("czIgppp/gijz+WtaHGTeh4ryjd1dOS7Pn6pJQa3VXZRnDL4DCdD68PBjLBjgZ7Li/LOhi20GwjFi6Y1YI5O6Zeaw6VzpH2BZAUtyBcsjZscIn93o99kPKG2ULnQJ2mRWWylAk7KsRUJAeDEqPG18wA==", response);
        }
    }
}
