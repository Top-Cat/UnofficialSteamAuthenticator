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
            SteamId = steamId;
            steamGuardAccount = Storage.GetSteamGuardAccount(steamId);

            Title = title;
            Content = content;
            Avatar = avatar;
        }

        internal void UpdateCode(long time)
        {
            Content = steamGuardAccount.FullyEnrolled ? steamGuardAccount.GenerateSteamGuardCodeForTime(time) : "2FA not setup";
            OnPropertyChanged();
        }
    }
}
