using System.Globalization;

namespace UnofficialSteamAuthenticator.Lib.Models
{
    /// <summary>
    ///     Type for showing languages in a list
    /// </summary>
    public class Language : ModelBase
    {
        private readonly CultureInfo culture;

        public string Code => this.culture.Name;

        public Language(string language)
        {
            this.culture = new CultureInfo(language);
        }

        public override string ToString()
        {
            return this.culture.NativeName;
        }
    }
}
