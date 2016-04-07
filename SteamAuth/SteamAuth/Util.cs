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

        public static string GenerateDeviceId()
        {
            IBuffer random = CryptographicBuffer.GenerateRandom(32);
            string random32 = CryptographicBuffer.EncodeToHexString(random).Replace("-", "").Substring(0, 32).ToLower();

            var ratios = new[]
            {
                8, 4, 4, 4, 12
            };

            return "android:" + SplitOnRatios(random32, ratios, "-");
        }

        public static ulong ConvertToSteam3(ulong steamid)
        {
            const ulong baseId = 76561197960265728;
            ulong result = steamid - baseId;

            if (result <= 0 || result >= 68719476736L)
                throw new ArgumentOutOfRangeException(nameof(steamid));

            return result;
        }

        public static string SplitOnRatios(string str, int[] ratios, string intermediate)
        {
            var result = "";
            var pos = 0;

            foreach (int ratio in ratios)
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
