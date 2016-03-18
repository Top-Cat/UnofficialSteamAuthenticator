using Windows.UI.Xaml.Media.Imaging;
using SteamAuth;

namespace UnofficialSteamAuthenticator.Models
{
    /// <summary>
    /// Type for showing users in a list
    /// </summary>
    internal class User : ModelBase
    {
        public ulong SteamId { get; }
        public string Title { get; set; }
        public string Content { get; set; }
        public BitmapImage Avatar { get; set; }
        private readonly SteamGuardAccount steamGuardAccount;

        public User(ulong steamId, string title, string content, BitmapImage avatar)
        {
            this.SteamId = steamId;
            this.steamGuardAccount = Storage.GetSteamGuardAccount(steamId);

            this.Title = title;
            this.Content = content;
            this.Avatar = avatar;
        }

        internal void UpdateCode(long time)
        {
            this.Content = this.steamGuardAccount.FullyEnrolled ? this.steamGuardAccount.GenerateSteamGuardCodeForTime(time) : "2FA not setup";
            this.OnPropertyChanged();
        }
    }
}
