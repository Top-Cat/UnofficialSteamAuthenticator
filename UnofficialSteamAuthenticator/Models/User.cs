using System;
using Windows.UI.Xaml.Media.Imaging;
using SteamAuth;

namespace UnofficialSteamAuthenticator.Models
{
    /// <summary>
    ///     Type for showing users in a list
    /// </summary>
    internal class User : ModelBase
    {
        private readonly SteamGuardAccount steamGuardAccount;
        private string content;
        private string title;

        public User(ulong steamId, string title, string content, BitmapImage avatar)
        {
            this.SteamId = steamId;
            this.steamGuardAccount = Storage.GetSteamGuardAccount(steamId);

            // skip notifying in constructor
            this.title = title;
            this.content = content;
            this.AccountName = this.steamGuardAccount.AccountName;
            this.Avatar = new Avatar(steamId);
        }

        public ulong SteamId { get; }

        public string Title
        {
            get { return this.title; }
            set
            {
                this.title = value;

                this.steamGuardAccount.DisplayName = value;
                Storage.PushStore(this.steamGuardAccount);

                this.OnPropertyChanged();
            }
        }

        public string Content
        {
            get { return this.content; }
            set
            {
                this.content = value;
                this.OnPropertyChanged();
            }
        }

        public Avatar Avatar { get; }
        public string AccountName { get; }

        internal void UpdateCode(long time)
        {
            this.Content = this.steamGuardAccount.FullyEnrolled ? this.steamGuardAccount.GenerateSteamGuardCodeForTime(time) : "2FA not setup";
        }
    }
}
