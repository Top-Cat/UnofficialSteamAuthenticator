using UnofficialSteamAuthenticator.SteamAuth;

namespace UnofficialSteamAuthenticator.Models
{
    /// <summary>
    ///     Type for showing users in a list
    /// </summary>
    internal class User : ModelBase
    {
        public SteamGuardAccount SteamGuardAccount { get; }
        private string content;
        private string title;

        public User(ulong steamId, long lastCurrent, string content)
        {
            this.SteamId = steamId;
            this.SteamGuardAccount = Storage.GetSteamGuardAccount(steamId);

            // skip notifying in constructor
            this.title = this.SteamGuardAccount.DisplayName;
            this.content = content;
            this.AccountName = this.SteamGuardAccount.AccountName ?? this.SteamGuardAccount.Session.Username;
            this.Avatar = new Avatar(steamId);
            this.LastCurrent = lastCurrent;
        }

        public ulong SteamId { get; }
        public long LastCurrent { get; }

        public string Title
        {
            get { return this.title; }
            set
            {
                this.title = value;

                this.SteamGuardAccount.DisplayName = value;
                Storage.PushStore(this.SteamGuardAccount);

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
            this.Content = this.SteamGuardAccount.FullyEnrolled ? this.SteamGuardAccount.GenerateSteamGuardCodeForTime(time) : "2FA not setup";
        }
    }
}
