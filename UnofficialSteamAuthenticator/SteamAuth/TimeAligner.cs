using Newtonsoft.Json;
using UnofficialSteamAuthenticator.Models;
using UnofficialSteamAuthenticator.Models.SteamAuth;

namespace UnofficialSteamAuthenticator.SteamAuth
{
    /// <summary>
    ///     Class to help align system time with the Steam server time. Not super advanced; probably not taking some things into account that it should.
    ///     Necessary to generate up-to-date codes. In general, this will have an error of less than a second, assuming Steam is operational.
    /// </summary>
    public class TimeAligner
    {
        private static bool _aligned;
        private static int _timeDifference;

        public static void GetSteamTime(IWebRequest web, LongCallback callback)
        {
            if (_aligned)
            {
                callback(Util.GetSystemUnixTime() + _timeDifference);
                return;
            }

            AlignTime(web, response =>
            {
                callback(Util.GetSystemUnixTime() + _timeDifference);
            });
        }

        public static void AlignTime(IWebRequest web, BCallback callback)
        {
            long currentTime = Util.GetSystemUnixTime();
            web.Request(APIEndpoints.TWO_FACTOR_TIME_QUERY, "POST", response =>
            {
                if (response != null)
                {
                    var query = JsonConvert.DeserializeObject<WebResponse<TimeQueryResponse>>(response);
                    _timeDifference = (int) (query.Response.ServerTime - currentTime);
                    _aligned = true;

                    callback(true);
                }
                else
                {
                    callback(false);
                }
            });
        }
    }
}
