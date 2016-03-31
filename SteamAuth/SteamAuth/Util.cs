using System;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace UnofficialSteamAuthenticator.Lib.SteamAuth
{
    public class Util
    {
        public static long GetSystemUnixTime()
        {
            return (long) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static string GenerateDeviceID()
        {
            IBuffer random = CryptographicBuffer.GenerateRandom(32);
            string random32 = CryptographicBuffer.EncodeToHexString(random).Replace("-", "").Substring(0, 32).ToLower();

            return "android:" + SplitOnRatios(random32, new[] { 8, 4, 4, 4, 12 }, "-");
        }

        public static string SplitOnRatios(string str, int[] ratios, string intermediate)
        {
            string result = "";
            int pos = 0;

            foreach (var ratio in ratios)
            {
                int sectionSize = Math.Min(ratio, str.Length - pos);

                if (sectionSize > 0)
                {
                    result += (pos > 0 ? intermediate : string.Empty) + str.Substring(pos, sectionSize);
                    pos += sectionSize;
                }
            }

            return result;
        }
    }
}
