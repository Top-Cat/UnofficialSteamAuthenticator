using System.Net;
using Newtonsoft.Json;

namespace SteamAuth
{

    /// <summary>
    /// Class to help align system time with the Steam server time. Not super advanced; probably not taking some things into account that it should.
    /// Necessary to generate up-to-date codes. In general, this will have an error of less than a second, assuming Steam is operational.
    /// </summary>
    public class TimeAligner
    {
        private static bool _aligned = false;
        private static int _timeDifference = 0;

        public static void GetSteamTime(IWebRequest web, LongCallback callback)
        {
            if (TimeAligner._aligned)
            {
                callback(Util.GetSystemUnixTime() + _timeDifference);
                return;
            }

            TimeAligner.AlignTime(web, response =>
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
                    TimeQuery query = JsonConvert.DeserializeObject<TimeQuery>(response);
                    TimeAligner._timeDifference = (int)(query.Response.ServerTime - currentTime);
                    TimeAligner._aligned = true;

                    callback(true);
                } else {
                    callback(false);
                }
            });
        }

        internal class TimeQuery
        {
            [JsonProperty("response")]
            internal TimeQueryResponse Response { get; set; }

            internal class TimeQueryResponse
            {
                [JsonProperty("server_time")]
                public long ServerTime { get; set; }
            }
            
        }
    }
}
