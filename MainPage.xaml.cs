using SteamAuth;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

namespace SteamAppNative
{
    public class MyModel
    {
        public string title { get; set; }
        public string content { get; set; }
        public BitmapImage image { get; set; }
    }

    public sealed partial class MainPage : Page
    {
        private SteamGuardAccount account;
        private string confUrl = "";
        private string confWebUrl = "";
        private string chatUrl = "https://steamcommunity.com/chat";

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        private void WebNotify(object sender, NotifyEventArgs e)
        {
            Debug.WriteLine("notify" + e.Value);
            HandleUri(new Uri(e.Value));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ConfirmationWeb.ScriptNotify += WebNotify;
            ConfirmationWeb.NavigationCompleted += InjectCode;
            HardwareButtons.BackPressed += BackPressed;

            this.account = Storage.SGAFromStore();
            SteamGuardButton_Click(null, null);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ConfirmationWeb.ScriptNotify -= WebNotify;
            ConfirmationWeb.NavigationCompleted -= InjectCode;
            HardwareButtons.BackPressed -= BackPressed;
        }

        private void BackPressed(object s, BackPressedEventArgs args)
        {
            if (ConfirmationWeb.Visibility == Visibility.Visible && ConfirmationWeb.CanGoBack && confWebUrl != confUrl)
            {
                ConfirmationWeb.GoBack();
                args.Handled = true;
            }
        }

        internal void HandleUri(Uri uri)
        {
            Dictionary<string, string> query = new Dictionary<string, string>();
            if (uri.Query.Length > 0)
            {
                WwwFormUrlDecoder decoder = new WwwFormUrlDecoder(uri.Query);
                query = decoder.ToDictionary(x => x.Name, x => x.Value);
            }

            switch (uri.Host)
            {
                case "settitle":
                    // I'm not entirely sure if this is applicable
                    // Sets values like title=Confirmations or title=Chat
                    break;
                case "steamguard":
                    if (query["op"] != "conftag") break;

                    account.GenerateConfirmationQueryParams(response =>
                    {
                        string[] args = { "window.SGHandler.update('" + response + "', 'ok');" };
                        ConfirmationWeb.InvokeScriptAsync("eval", args);
                    }, query["arg1"]);

                    break;
                default:
                    Debug.WriteLine("Unhandled uri: " + uri.AbsoluteUri);
                    break;
            }
        }

        private void steamGuardUpdate_Tick(object sender, object e)
        {
            if (SteamGuardGrid.Visibility != Visibility.Visible) return;

            TimeAligner.GetSteamTime(time =>
            {
                ulong currentChunk = (ulong)time / 30L;
                long timeRemaining = 31 - (time % 30);
                Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    var storyboard = new Storyboard();
                    var animation = new DoubleAnimation { Duration = TimeSpan.FromSeconds(timeRemaining), From = timeRemaining, To = 0, EnableDependentAnimation = true };
                    Storyboard.SetTarget(animation, SteamGuardTimer);
                    Storyboard.SetTargetProperty(animation, "Value");
                    storyboard.Children.Add(animation);
                    storyboard.Completed += steamGuardUpdate_Tick;
                    storyboard.Begin();

                    SteamGuardCode.Text = account.GenerateSteamGuardCodeForTime(time);
                });
            });
        }

        private void SteamGuardButton_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            if (account == null || !account.FullyEnrolled)
            {
                LinkGrid.Visibility = Visibility.Visible;
                return;
            }

            RevCode.Text = account.RevocationCode;
            SteamGuardGrid.Visibility = Visibility.Visible;
            steamGuardUpdate_Tick(null, null);
        }

        private void MessageButton_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            ChatWeb.Visibility = Visibility.Visible;

            ChatWeb.NavigateWithHttpRequestMessage(GetMessageForUrl(chatUrl));
        }

        private void ConfirmationsButton_Click(object sender, RoutedEventArgs e)
        {
            HideAll();

            ConfirmationWeb.Visibility = Visibility.Visible;

            account.GenerateConfirmationURL(response =>
            {
                confUrl = response;
                HttpRequestMessage message = GetMessageForUrl(response);
                
                Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ConfirmationWeb.NavigateWithHttpRequestMessage(message);
                });
            });
        }

        private HttpRequestMessage GetMessageForUrl(string url)
        {
            CookieContainer cookies = new CookieContainer();
            account.Session.AddCookies(cookies);

            Uri baseUri = new Uri(url);
            Windows.Web.Http.Filters.HttpBaseProtocolFilter filter = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
            foreach (Cookie c in cookies.GetCookies(SteamWeb.uri))
            {
                Windows.Web.Http.HttpCookie cookie = new Windows.Web.Http.HttpCookie(c.Name, c.Domain, c.Path);
                cookie.Value = c.Value;
                filter.CookieManager.SetCookie(cookie, false);
            }

            return new HttpRequestMessage(Windows.Web.Http.HttpMethod.Get, baseUri);
        }

        private void InjectCode(object sender, WebViewNavigationCompletedEventArgs e)
        {
            confWebUrl = e.Uri.AbsoluteUri;

            if (confWebUrl == confUrl)
            {
                // Try to only inject this once
                string[] args = { @"
                    if (!window.SGHandler) {
                        window.SGHandler = {
                            Value: 0,
                            Status: 'busy',
                            getResultValue: function() {
                                return window.SGHandler.Value;
                            },
                            getResultStatus: function() {
                                return window.SGHandler.Status;
                            },
                            getResultCode: function() {
                                return 0;
                            },
                            update: function(a, b) {
                                window.SGHandler.Value = a;
                                window.SGHandler.Status = b;
                            }
                        };
                        runLocalUrlO = runLocalUrl;
                        runLocalUrl = function(url) {
                            window.SGHandler.Status = 'busy';
                            runLocalUrlO(url);
                        }
                    }"
                };
                ConfirmationWeb.InvokeScriptAsync("eval", args);
            }
        }

        private void HideAll()
        {
            SteamGuardGrid.Visibility = LinkGrid.Visibility = ConfirmationWeb.Visibility = ChatWeb.Visibility = Visibility.Collapsed;
        }

        private void LinkBtn_Click(object sender, RoutedEventArgs e)
        {
            if (account == null || !account.FullyEnrolled)
            {
                Frame.Navigate(typeof(Authenticator));
            }
        }

        private void UnlinkBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MessageDialog("Are you sure? This will incur a 7 day trade ban.");
            dialog.Title = "Unlink?";
            dialog.Commands.Add(new UICommand("Ok", new UICommandInvokedHandler(DoUnlink)));
            dialog.Commands.Add(new UICommand("Cancel")); // Take no action
            dialog.ShowAsync();
        }

        private void DoUnlink(IUICommand cmd)
        {
            account.DeactivateAuthenticator(response => {
                if (response)
                {
                    account.FullyEnrolled = false;
                    Storage.PushStore(account);
                    Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        SteamGuardButton_Click(null, null);
                    });
                }
                else
                {
                    var dialog = new MessageDialog("Failed to unlink authenticator");
                    dialog.Title = "Error";
                    dialog.Commands.Add(new UICommand("Ok"));
                    dialog.ShowAsync();
                }
            });
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Storage.SDClearStore();
            Frame.Navigate(typeof(Login));
        }
    }
}
