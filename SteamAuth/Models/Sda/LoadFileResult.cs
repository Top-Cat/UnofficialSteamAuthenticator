using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnofficialSteamAuthenticator.Lib.Models.Sda
{
    public class LoadFileResult : ModelBase
    {
        public ELoadFileResult ResultStatus;
        public int Loaded;

        public LoadFileResult(ELoadFileResult resultStatus, int loaded)
        {
            this.ResultStatus = resultStatus;
            this.Loaded = loaded;
        }
    }
}
