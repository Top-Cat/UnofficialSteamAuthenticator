using System.Globalization;

namespace UnofficialSteamAuthenticator.Models
{
    /// <summary>
    ///     Type for showing languages in a list
    /// </summary>
    internal class Language : ModelBase
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
