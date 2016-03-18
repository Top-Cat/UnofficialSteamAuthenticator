using Newtonsoft.Json;
using SteamAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using UnofficialSteamAuthenticator.Models;

namespace UnofficialSteamAuthenticator
{
    public sealed partial class UsersPage
    {
        private readonly Dictionary<ulong, User> listElems = new Dictionary<ulong, User>();
        private Storyboard storyboard;

        public UsersPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += this.BackPressed;

            this.listElems.Clear();
            this.AccountList.Items.Clear();

            this.steamGuardUpdate_Tick(null, null);

            Dictionary<ulong, SessionData> accs = Storage.GetAccounts();
            if (accs.Count < 1)
            {
                return;
            }

            string ids = string.Empty;
            foreach (ulong steamId in accs.Keys)
            {
                ids += steamId + ",";

                //SteamGuardAccount steamGuardAccount = Storage.GetSteamGuardAccount(steamId);
                User usr = new User(steamId, string.Empty, "Generating Code...", null);
                this.listElems.Add(steamId, usr);
                this.AccountList.Items.Add(usr);
            }

            string accessToken = accs.First().Value.OAuthToken;
            SteamWeb.Request(async responseString =>
            {
                var responseObj = JsonConvert.DeserializeObject<Players>(responseString);
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    foreach (Player p in responseObj.PlayersList)
                    {
                        if (this.listElems.ContainsKey(p.SteamId))
                        {
                            this.listElems[p.SteamId].Avatar = new BitmapImage(new Uri(p.AvatarUri));
                            this.listElems[p.SteamId].Title = p.Username;
                            this.listElems[p.SteamId].OnPropertyChanged();
                        }
                    }
                });
            }, APIEndpoints.USER_SUMMARIES_URL + "?access_token=" + accessToken + "&steamids=" + ids, "GET");
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= this.BackPressed;
        }

        private void BackPressed(object sender, BackPressedEventArgs args)
        {
            if (this.Frame.CanGoBack)
            {
                args.Handled = true;
                this.Frame.GoBack();
            }
        }

        private void AccountList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var user = (User)e.ClickedItem;
            Storage.SetCurrentUser(user.SteamId);
            this.Frame.Navigate(typeof(MainPage));
        }

        private void steamGuardUpdate_Tick(object sender, object e)
        {
            if (this.storyboard != null)
            {
                this.storyboard.Stop();
            }

            TimeAligner.GetSteamTime(async time =>
            {
                long timeRemaining = 31 - (time % 30);
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    this.storyboard = new Storyboard();
                    var animation = new DoubleAnimation { Duration = TimeSpan.FromSeconds(timeRemaining), From = timeRemaining, To = 0, EnableDependentAnimation = true };
                    Storyboard.SetTarget(animation, this.SteamGuardTimer);
                    Storyboard.SetTargetProperty(animation, "Value");
                    this.storyboard.Children.Add(animation);
                    this.storyboard.Completed += this.steamGuardUpdate_Tick;
                    this.storyboard.Begin();

                    foreach (ulong steamid in this.listElems.Keys)
                    {
                        this.listElems[steamid].UpdateCode(time);
                    }
                });
            });
        }

        private void NewUser_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(LoginPage));
        }
    }
}
