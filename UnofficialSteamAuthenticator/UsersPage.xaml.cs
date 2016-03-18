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
        private readonly Dictionary<ulong, User> _listElems = new Dictionary<ulong, User>();
        private Storyboard _storyboard;

        public UsersPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += BackPressed;

            _listElems.Clear();
            AccountList.Items.Clear();

            steamGuardUpdate_Tick(null, null);

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
                _listElems.Add(steamId, usr);
                AccountList.Items.Add(usr);
            }

            string accessToken = accs.First().Value.OAuthToken;
            SteamWeb.Request(async responseString =>
            {
                var responseObj = JsonConvert.DeserializeObject<Players>(responseString);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    foreach (Player p in responseObj.PlayersList)
                    {
                        if (_listElems.ContainsKey(p.SteamId))
                        {
                            _listElems[p.SteamId].Avatar = new BitmapImage(new Uri(p.AvatarUri));
                            _listElems[p.SteamId].Title = p.Username;
                            _listElems[p.SteamId].OnPropertyChanged();
                        }
                    }
                });
            }, APIEndpoints.USER_SUMMARIES_URL + "?access_token=" + accessToken + "&steamids=" + ids, "GET");
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= BackPressed;
        }

        private void BackPressed(object sender, BackPressedEventArgs args)
        {
            if (Frame.CanGoBack)
            {
                args.Handled = true;
                Frame.GoBack();
            }
        }

        private void AccountList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var user = (User)e.ClickedItem;
            Storage.SetCurrentUser(user.SteamId);
            Frame.Navigate(typeof(MainPage));
        }

        private void steamGuardUpdate_Tick(object sender, object e)
        {
            if (_storyboard != null)
            {
                _storyboard.Stop();
            }

            TimeAligner.GetSteamTime(async time =>
            {
                //ulong currentChunk = (ulong)time / 30L;
                long timeRemaining = 31 - (time % 30);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    _storyboard = new Storyboard();
                    var animation = new DoubleAnimation { Duration = TimeSpan.FromSeconds(timeRemaining), From = timeRemaining, To = 0, EnableDependentAnimation = true };
                    Storyboard.SetTarget(animation, SteamGuardTimer);
                    Storyboard.SetTargetProperty(animation, "Value");
                    _storyboard.Children.Add(animation);
                    _storyboard.Completed += steamGuardUpdate_Tick;
                    _storyboard.Begin();

                    foreach (ulong steamid in _listElems.Keys)
                    {
                        _listElems[steamid].UpdateCode(time);
                    }
                });
            });
        }

        private void NewUser_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoginPage));
        }
    }
}
