namespace UnofficialSteamAuthenticator.Lib.SteamAuth
{
    public class Confirmation
    {
        public enum ConfirmationType
        {
            GenericConfirmation,
            Trade,
            MarketSellTransaction,
            Unknown
        }

        public string Description;
        public string Description2;
        public string DescriptionTime;
        public int ID;
        public string Key;

        public ConfirmationType ConfType
        {
            get
            {
                if (string.IsNullOrEmpty(this.Description)) return ConfirmationType.Unknown;
                if (this.Description.StartsWith("Confirm ")) return ConfirmationType.GenericConfirmation;
                if (this.Description.StartsWith("Trade with ")) return ConfirmationType.Trade;
                if (this.Description.StartsWith("Sell -")) return ConfirmationType.MarketSellTransaction;

                return ConfirmationType.Unknown;
            }
        }
    }
}
