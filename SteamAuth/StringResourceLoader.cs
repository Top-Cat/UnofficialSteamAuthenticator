using System;
using Windows.ApplicationModel.Resources;

namespace UnofficialSteamAuthenticator.Lib
{
    public class StringResourceLoader
    {
        public static string GetString(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            var loader = new ResourceLoader();
            return loader.GetString(key);
        }
    }
}
