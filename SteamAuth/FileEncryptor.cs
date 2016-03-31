using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace UnofficialSteamAuthenticator.Lib
{
    /// <summary>
    /// This class provides the controls that will encrypt and decrypt the *.maFile files
    /// 
    /// Passwords entered will be passed into 100k rounds of PBKDF2 (RFC2898) with a cryptographically random salt.
    /// The generated key will then be passed into AES-256 (RijndalManaged) which will encrypt the data
    /// in cypher block chaining (CBC) mode, and then write both the PBKDF2 salt and encrypted data onto the disk.
    /// </summary>
    public static class FileEncryptor
    {
        private const int PBKDF2_ITERATIONS = 50000; //Set to 50k to make program not unbearably slow. May increase in future.
        private const int SALT_LENGTH = 8;
        private const int KEY_SIZE_BYTES = 32;
        private const int IV_LENGTH = 16;

        /// <summary>
        /// Returns an 8-byte cryptographically random salt in base64 encoding
        /// </summary>
        /// <returns></returns>
        public static string GetRandomSalt()
        {
            return CryptographicBuffer.EncodeToBase64String(CryptographicBuffer.GenerateRandom(SALT_LENGTH));
        }

        /// <summary>
        /// Returns a 16-byte cryptographically random initialization vector (IV) in base64 encoding
        /// </summary>
        /// <returns></returns>
        public static string GetInitializationVector()
        {
            return CryptographicBuffer.EncodeToBase64String(CryptographicBuffer.GenerateRandom(IV_LENGTH));
        }


        /// <summary>
        /// Generates an encryption key derived using a password, a random salt, and specified number of rounds of PBKDF2
        /// 
        /// TODO: pass in password via SecureString?
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        public static IBuffer GetEncryptionKey(string password, string salt)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password is empty");
            }
            if (salt == null || salt.Length == 0)
            {
                throw new ArgumentException("Salt is empty");
            }

            KeyDerivationAlgorithmProvider pbkdf2 = KeyDerivationAlgorithmProvider.OpenAlgorithm(KeyDerivationAlgorithmNames.Pbkdf2Sha1);

            IBuffer buffSecret = CryptographicBuffer.ConvertStringToBinary(password, BinaryStringEncoding.Utf8);
            CryptographicKey key = pbkdf2.CreateKey(buffSecret);

            KeyDerivationParameters parameters = KeyDerivationParameters.BuildForPbkdf2(CryptographicBuffer.DecodeFromBase64String(salt), PBKDF2_ITERATIONS);

            return CryptographicEngine.DeriveKeyMaterial(key, parameters, KEY_SIZE_BYTES);
        }

        /// <summary>
        /// Tries to decrypt and return data given an encrypted base64 encoded string. Must use the same
        /// password, salt, IV, and ciphertext that was used during the original encryption of the data.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="passwordSalt"></param>
        /// <param name="IV">Initialization Vector</param>
        /// <param name="encryptedData"></param>
        /// <returns></returns>
        public static string DecryptData(string password, string passwordSalt, string IV, string encryptedData)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password is empty");
            }
            if (string.IsNullOrEmpty(passwordSalt))
            {
                throw new ArgumentException("Salt is empty");
            }
            if (string.IsNullOrEmpty(IV))
            {
                throw new ArgumentException("Initialization Vector is empty");
            }
            if (string.IsNullOrEmpty(encryptedData))
            {
                throw new ArgumentException("Encrypted data is empty");
            }

            try
            {
                IBuffer cipherText = CryptographicBuffer.DecodeFromBase64String(encryptedData);
                IBuffer iv = CryptographicBuffer.DecodeFromBase64String(IV);
                IBuffer key = GetEncryptionKey(password, passwordSalt);

                SymmetricKeyAlgorithmProvider aes256 = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);
                CryptographicKey k = aes256.CreateSymmetricKey(key);

                IBuffer decrypted = CryptographicEngine.Decrypt(k, cipherText, iv);

                return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, decrypted);
            }
            catch (Exception)
            {
                //Data error (cyclic redundancy check). (Exception from HRESULT: 0x80070017)
            }
            return null;
        }

        /// <summary>
        /// Encrypts a string given a password, salt, and initialization vector, then returns result in base64 encoded string.
        /// 
        /// To retrieve this data, you must decrypt with the same password, salt, IV, and cyphertext that was used during encryption
        /// </summary>
        /// <param name="password"></param>
        /// <param name="passwordSalt"></param>
        /// <param name="IV"></param>
        /// <param name="plaintext"></param>
        /// <returns></returns>
        public static string EncryptData(string password, string passwordSalt, string IV, string plaintext)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password is empty");
            }
            if (string.IsNullOrEmpty(passwordSalt))
            {
                throw new ArgumentException("Salt is empty");
            }
            if (string.IsNullOrEmpty(IV))
            {
                throw new ArgumentException("Initialization Vector is empty");
            }
            if (string.IsNullOrEmpty(plaintext))
            {
                throw new ArgumentException("Plaintext data is empty");
            }
            IBuffer plainText = CryptographicBuffer.CreateFromByteArray(Encoding.UTF8.GetBytes(plaintext));
            IBuffer iv = CryptographicBuffer.DecodeFromBase64String(IV);
            IBuffer key = GetEncryptionKey(password, passwordSalt);

            SymmetricKeyAlgorithmProvider aes256 = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);
            CryptographicKey k = aes256.CreateSymmetricKey(key);

            IBuffer encrypted = CryptographicEngine.Encrypt(k, plainText, iv);

            return CryptographicBuffer.EncodeToBase64String(encrypted);
        }
    }
}