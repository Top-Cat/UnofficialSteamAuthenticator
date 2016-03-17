using Newtonsoft.Json;
using SteamAuth;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace UnofficialSteamAuthenticator
{

    public sealed partial class Users : Page
    {
        private Dictionary<ulong, User> listElems = new Dictionary<ulong, User>();
        private Storyboard storyboard;

        public Users()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += BackPressed;

            listElems.Clear();
            AccountList.Items.Clear();
            steamGuardUpdate_Tick(null, null);

            Dictionary<ulong, SessionData> accs = Storage.GetAccounts();
            if (accs.Count < 1) return;

            string ids = "";
            foreach (ulong steamid in accs.Keys)
            {
                ids += steamid + ",";

                SteamGuardAccount steamGuard = Storage.SGAFromStore(steamid);
                User usr = new User(steamid, "", "Generating Code...", null);
                listElems.Add(steamid, usr);
                AccountList.Items.Add(usr);
            }

            string access_token = accs.First().Value.OAuthToken;
            SteamWeb.Request(async response =>
            {
                Players r = JsonConvert.DeserializeObject<Players>(response);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    foreach (Player p in r.players)
                    {
                        if (listElems.ContainsKey(p.steamID))
                        {
                            listElems[p.steamID].avatar = new BitmapImage(new Uri(p.avatarUri));
                            listElems[p.steamID].title = p.username;
                            listElems[p.steamID].triggerUpdate();
                        }
                    }
                });
            }, APIEndpoints.USER_SUMMARIES_URL + "?access_token=" + access_token + "&steamids=" + ids, "GET");
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= BackPressed;
        }

        private void BackPressed(object sender, BackPressedEventArgs args)
        {
            if (Frame.CanGoBack) {
                args.Handled = true;
                Frame.GoBack();
            }
        }

        private void AccountList_ItemClick(object sender, ItemClickEventArgs e)
        {
            User usr = (User) e.ClickedItem;
            Storage.SetCurrentUser(usr.steamid);
            Frame.Navigate(typeof(MainPage));
        }

        private class Players
        {
            [JsonProperty("players")]
            public List<Player> players { get; set; }
        }

        private class Player
        {
            [JsonProperty("steamid")]
            public ulong steamID { get; set; }

            [JsonProperty("personaname")]
            public string username { get; set; }

            [JsonProperty("avatarfull")]
            public string avatarUri { get; set; }
        }

        // Type for showing users in a list
        private class User : INotifyPropertyChanged
        {
            public ulong steamid { get; }
            public string title { get; set; }
            public string content { get; set; }
            public BitmapImage avatar { get; set; }
            private SteamGuardAccount acc;

            public event PropertyChangedEventHandler PropertyChanged;

            public User(ulong steamid, string title, string content, BitmapImage avatar)
            {
                this.steamid = steamid;
                this.acc = Storage.SGAFromStore(steamid);

                this.title = title;
                this.content = content;
                this.avatar = avatar;
            }

            public void triggerUpdate()
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(""));
                }
            }

            internal void updateCode(long time)
            {
                this.content = acc.FullyEnrolled ? acc.GenerateSteamGuardCodeForTime(time) : "2FA not setup";
                triggerUpdate();
            }
        }

        private void steamGuardUpdate_Tick(object sender, object e)
        {
            if (storyboard != null) storyboard.Stop();

            TimeAligner.GetSteamTime(async time =>
            {
                ulong currentChunk = (ulong)time / 30L;
                long timeRemaining = 31 - (time % 30);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    storyboard = new Storyboard();
                    var animation = new DoubleAnimation { Duration = TimeSpan.FromSeconds(timeRemaining), From = timeRemaining, To = 0, EnableDependentAnimation = true };
                    Storyboard.SetTarget(animation, SteamGuardTimer);
                    Storyboard.SetTargetProperty(animation, "Value");
                    storyboard.Children.Add(animation);
                    storyboard.Completed += steamGuardUpdate_Tick;
                    storyboard.Begin();

                    foreach (ulong steamid in listElems.Keys)
                    {
                        listElems[steamid].updateCode(time);
                    }
                });
            });
        }

        private void NewUser_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Login));
        }
    }
}
