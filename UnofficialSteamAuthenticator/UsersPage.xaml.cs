using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Phone.UI.Input;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using UnofficialSteamAuthenticator.Lib.SteamAuth;
using UnofficialSteamAuthenticator.Lib.Models;
using UnofficialSteamAuthenticator.Lib;
using Windows.Storage;
using System.IO;
using Newtonsoft.Json;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Activation;

namespace UnofficialSteamAuthenticator
{
    public sealed partial class UsersPage
    {
        private readonly Dictionary<ulong, User> listElems = new Dictionary<ulong, User>();
        private readonly SteamWeb web = ((App) Application.Current).SteamWeb;
        private Storyboard storyboard;

        public UsersPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += this.BackPressed;

            this.listElems.Clear();

            this.steamGuardUpdate_Tick(null, null);

            Dictionary<ulong, SessionData> accs = Storage.GetAccounts();
            if (accs.Count < 1)
            {
                return;
            }

            foreach (ulong steamId in accs.Keys)
            {
                var usr = new User(steamId, accs[steamId].LastCurrent, StringResourceLoader.GetString("GeneratingCode_Ellipsis"));
                this.listElems.Add(steamId, usr);
            }

            this.AccountList.ItemsSource = this.listElems.Values.OrderByDescending(item => item.LastCurrent).ToList();

            UserSummary.GetSummaries(this.web, accs.First().Value, accs.Keys.ToArray(), this.SummariesCallback);
        }

        private async void SummariesCallback(Players responseObj)
        {
            if (responseObj?.PlayersList == null)
            {
                return;
            }

            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (Player p in responseObj.PlayersList)
                {
                    if (this.listElems.ContainsKey(p.SteamId))
                    {
                        this.listElems[p.SteamId].Avatar.SetUri(new Uri(p.AvatarUri));
                        this.listElems[p.SteamId].Title = p.Username;
                    }
                }
            });
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= this.BackPressed;
        }

        private void BackPressed(object sender, BackPressedEventArgs args)
        {
            if (!this.Frame.CanGoBack)
                return;

            args.Handled = true;
            this.Frame.GoBack();
        }

        private void AccountList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var user = (User) e.ClickedItem;
            Storage.SetCurrentUser(user.SteamId);
            this.Frame.Navigate(typeof(MainPage));
        }

        private void steamGuardUpdate_Tick(object sender, object e)
        {
            try
            {
                this.storyboard?.Stop();
            }
            catch (NotImplementedException)
            {
                // Not sure why this happens
            }

            TimeAligner.GetSteamTime(this.web, async time =>
            {
                var timeRemaining = (byte) (31 - time % 30);
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.storyboard = new Storyboard();
                    var animation = new DoubleAnimation
                    {
                        Duration = TimeSpan.FromSeconds(timeRemaining),
                        From = timeRemaining,
                        To = 0,
                        EnableDependentAnimation = true
                    };
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

        private void NewUser_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(LoginPage));
        }

        private void AccountList_OnHolding(object sender, HoldingRoutedEventArgs e)
        {
            var senderElement = sender as FrameworkElement;
            // If you need the clicked element:
            // Item whichOne = senderElement.DataContext as Item;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            flyoutBase.ShowAt(senderElement);
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement) e.OriginalSource;
            var selectedOne = (User) element.DataContext;
            if (selectedOne == null)
                return;

            var folderPicker = new FolderPicker();
            folderPicker.ContinuationData["user"] = selectedOne.SteamId;
            folderPicker.PickFolderAndContinue();
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement) e.OriginalSource;
            var selectedOne = (User) element.DataContext;
            if (selectedOne == null)
                return;

            var dialog = new MessageDialog(StringResourceLoader.GetString("User_Logout_Prompt_Message"))
            {
                Title = StringResourceLoader.GetString("User_Logout_Prompt_Title")
            };
            dialog.Commands.Add(new UICommand(StringResourceLoader.GetString("UiCommand_Yes_Text"), cmd =>
            {
                Storage.Logout(selectedOne.SteamId);
                this.AccountList.Items?.Remove(selectedOne);
            }));
            dialog.Commands.Add(new UICommand(StringResourceLoader.GetString("UiCommand_No_Text"))); // Take no action
            await dialog.ShowAsync();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }

        #region loadMAFile
        CoreApplicationView view = CoreApplication.GetCurrentView();
        string loadedjson;
        int status = 0;
        // Integer showing status for loadButton, from recieved file.
        // 0 - none
        // 1 - file loaded properly.
        // 2 - error.
        private async void loadButton_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog msgbox = new MessageDialog("Pick .maFile to work with.");
            SteamGuardAccount recieved;
            FileOpenPicker openPicker = new FileOpenPicker();
            await msgbox.ShowAsync();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".maFile");
            openPicker.PickSingleFileAndContinue();
            view.Activated += viewActivated;
            /*
            While loop, to stop code here until recieved file.
            if not - stops void.
            TopCat - if you'll find better way - change it ;)
            */
            while (true)
            {
                if (status==1)
                {
                    break;
                }
                if (status==2)
                {
                    return;
                }
            }
            /*
            If file is properly loaded, we go to moving it to SteamGuardAccount object.
            Even if it is empty, or couldn't be properly moved to json format - app
            catches it and stops loading progress.
             */
            try
            {
                recieved = JsonConvert.DeserializeObject<SteamGuardAccount>(loadedjson);
                Storage.PushStore(recieved);
                msgbox.Content = ("File loaded successfully.");
                await msgbox.ShowAsync();
            }
            catch (Exception)
            {
                msgbox.Content = ("Error loading account from file");
                await msgbox.ShowAsync();
            }
            /*
            TODO:
            If loaded properly, I don't exactly know how to "reload" your account
            list. This code seems a bit too complicated for me, so I don't touch it.
            */
        }
        /*
        This void occurs when app is unfreezed by file picker. It checks the file
        and returns state. If wrong - shows message box.
        This crap is indeed on WP 8.1. On WP10 we could use FileOpenPicker.PickSingleFileAsync()
        and get file in one void. 
        */
        private async void viewActivated(CoreApplicationView sender, IActivatedEventArgs args1)
        {
            FileOpenPickerContinuationEventArgs args = args1 as FileOpenPickerContinuationEventArgs;

            try
            {
                if (args != null)
                {
                    if (args.Files.Count == 0) return;
                    view.Activated -= viewActivated;
                    StorageFile file = args.Files[0];
                    using (StreamReader sRead = new StreamReader(await file.OpenStreamForReadAsync()))
                    {
                        loadedjson = await sRead.ReadToEndAsync();
                        if (loadedjson == null)
                        {
                            status = 2;
                        }
                    }
                }
            }
            catch (Exception)
            {
                MessageDialog msgbox = new MessageDialog("Error loading file.");
                await msgbox.ShowAsync();
                status = 2;
            }
            
        }
        #endregion
    }
}
