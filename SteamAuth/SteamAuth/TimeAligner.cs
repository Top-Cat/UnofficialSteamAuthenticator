using System.Net;
using Newtonsoft.Json;
using UnofficialSteamAuthenticator.Lib.Models;
using UnofficialSteamAuthenticator.Lib.Models.SteamAuth;

namespace UnofficialSteamAuthenticator.Lib.SteamAuth
{
    /// <summary>
    ///     Class to help align system time with the Steam server time. Not super advanced; probably not taking some things
    ///     into account that it should.
    ///     Necessary to generate up-to-date codes. In general, this will have an error of less than a second, assuming Steam
    ///     is operational.
    /// </summary>
    public class TimeAligner
    {
        private static bool aligned;
        private static int timeDifference;

        public static void GetSteamTime(IWebRequest web, LongCallback callback)
        {
            if (aligned)
            {
                callback(Util.GetSystemUnixTime() + timeDifference);
                return;
            }

            AlignTime(web, response =>
            {
                callback(Util.GetSystemUnixTime() + timeDifference);
            });
        }

        public static void AlignTime(IWebRequest web, BCallback callback)
        {
            long currentTime = Util.GetSystemUnixTime();
            web.Request(ApiEndpoints.TWO_FACTOR_TIME_QUERY, "POST", (response, code) =>
            {
                if (response != null && code == HttpStatusCode.OK)
                {
                    var query = JsonConvert.DeserializeObject<WebResponse<TimeQueryResponse>>(response);
                    timeDifference = (int) (query.Response.ServerTime - currentTime);
                    aligned = true;

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
