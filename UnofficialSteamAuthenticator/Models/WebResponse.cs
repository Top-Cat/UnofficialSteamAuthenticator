using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Models
{
    internal class WebResponse<T> : ModelBase
    {
        [JsonProperty("response")]
        public T Response { get; set; }
    }
}
