using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Lib.Models
{
    public class Player : ModelBase
    {
        [JsonProperty("steamid")]
        public ulong SteamId { get; set; }

        [JsonProperty("personaname")]
        public string Username { get; set; }

        [JsonProperty("avatarfull")]
        public string AvatarUri { get; set; }
    }
}
