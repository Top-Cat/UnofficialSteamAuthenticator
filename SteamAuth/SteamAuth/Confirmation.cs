namespace UnofficialSteamAuthenticator.Lib.SteamAuth
{
    public class Confirmation
    {
        public int ID;
        public string Key;
        public string Description;
        public string Description2;
        public string DescriptionTime;

        public ConfirmationType ConfType
        {
            get
            {
                if (string.IsNullOrEmpty(Description)) return ConfirmationType.Unknown;
                if (Description.StartsWith("Confirm ")) return ConfirmationType.GenericConfirmation;
                if (Description.StartsWith("Trade with ")) return ConfirmationType.Trade;
                if (Description.StartsWith("Sell -")) return ConfirmationType.MarketSellTransaction;

                return ConfirmationType.Unknown;
            }
        }

        public enum ConfirmationType
        {
            GenericConfirmation,
            Trade,
            MarketSellTransaction,
            Unknown
        }
    }
}
