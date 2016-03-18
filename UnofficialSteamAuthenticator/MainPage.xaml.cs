using SteamAuth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Windows.Foundation;
using Windows.Phone.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

namespace UnofficialSteamAuthenticator
{
    public sealed partial class MainPage
    {
        private SteamGuardAccount _account;
        private Storyboard _storyboard;
        private string _confUrl = string.Empty;
        private string _confWebUrl = string.Empty;
        private const string ChatUrl = "https://steamcommunity.com/chat";

        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;
        }

        private void WebNotify(object sender, NotifyEventArgs e)
        {
            HandleUri(new Uri(e.Value));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ConfirmationWeb.ScriptNotify += WebNotify;
            ConfirmationWeb.NavigationCompleted += InjectCode;
            HardwareButtons.BackPressed += BackPressed;

            _account = Storage.GetSteamGuardAccount();
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
            if (ConfirmationWeb.Visibility == Visibility.Visible && ConfirmationWeb.CanGoBack && _confWebUrl != _confUrl)
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
                case "lostauth":
                    // This code had a massive tantrum when run outside the application's thread
                    _account.RefreshSession(async success =>
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            if (success)
                            {
                                Storage.PushStore(_account.Session);
                                SteamGuardButton_Click(null, null);
                            }
                            else
                            {
                                LogoutButton_Click(null, null);
                            }
                        });
                    });
                    break;
                case "steamguard":
                    if (query["op"] != "conftag")
                    {
                        break;
                    }

                    _account.GenerateConfirmationQueryParams(async response =>
                    {
                        try
                        {
                            string[] args = { "window.SGHandler.update('" + response + "', 'ok');" };
                            await ConfirmationWeb.InvokeScriptAsync("eval", args);
                        }
                        catch (Exception)
                        {
                            // We're probably here because the webview was unloaded
                            // Just reload the view
                            ConfirmationsButton_Click(null, null);
                        }
                    }, query["arg1"]);

                    break;
                default:
                    Debug.WriteLine("Unhandled uri: " + uri.AbsoluteUri);
                    break;
            }
        }

        private void steamGuardUpdate_Tick(object sender, object e)
        {
            if (_storyboard != null)
            {
                _storyboard.Stop();
            }

            if (SteamGuardGrid.Visibility != Visibility.Visible)
            {
                return;
            }

            TimeAligner.GetSteamTime(async time =>
            {
                ulong currentChunk = (ulong)time / 30L;
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

                    SteamGuardCode.Text = _account.GenerateSteamGuardCodeForTime(time);
                });
            });
        }

        private void SteamGuardButton_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            if (_account == null || !_account.FullyEnrolled)
            {
                LinkGrid.Visibility = Visibility.Visible;
                return;
            }

            AccountText.Text = _account.AccountName ?? string.Empty;
            SteamGuardGrid.Visibility = Visibility.Visible;
            steamGuardUpdate_Tick(null, null);
        }

        private void MessageButton_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            ChatWeb.Visibility = Visibility.Visible;

            ChatWeb.NavigateWithHttpRequestMessage(GetMessageForUrl(ChatUrl));
        }

        private void ConfirmationsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_account == null || !_account.FullyEnrolled)
            {
                return;
            }

            HideAll();

            ConfirmationWeb.Visibility = Visibility.Visible;

            _account.GenerateConfirmationURL(async response =>
            {
                _confUrl = response;
                HttpRequestMessage message = GetMessageForUrl(response);

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ConfirmationWeb.NavigateWithHttpRequestMessage(message);
                });
            });
        }

        private HttpRequestMessage GetMessageForUrl(string url)
        {
            CookieContainer cookies = new CookieContainer();
            _account.Session.AddCookies(cookies);

            Uri baseUri = new Uri(url);
            Windows.Web.Http.Filters.HttpBaseProtocolFilter filter = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
            foreach (Cookie c in cookies.GetCookies(SteamWeb.uri))
            {
                HttpCookie cookie = new HttpCookie(c.Name, c.Domain, c.Path);
                cookie.Value = c.Value;
                filter.CookieManager.SetCookie(cookie, false);
            }

            return new HttpRequestMessage(HttpMethod.Get, baseUri);
        }

        private async void InjectCode(object sender, WebViewNavigationCompletedEventArgs e)
        {
            _confWebUrl = e.Uri.AbsoluteUri;

            if (e.IsSuccess && _confWebUrl == _confUrl)
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
                            window.SGHandler.update(0, 'busy');
                            runLocalUrlO(url);
                        }
                    }"
                };
                await ConfirmationWeb.InvokeScriptAsync("eval", args);
            }
        }

        private void HideAll()
        {
            SteamGuardGrid.Visibility = LinkGrid.Visibility = ConfirmationWeb.Visibility = ChatWeb.Visibility = Visibility.Collapsed;
        }

        private void LinkBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_account == null || !_account.FullyEnrolled)
            {
                Frame.Navigate(typeof(AuthenticatorPage));
            }
        }

        private async void UnlinkBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MessageDialog("Are you sure? This will incur trade holds for at least 7 days.");
            dialog.Title = "Unlink?";
            dialog.Commands.Add(new UICommand("Ok", DoUnlink));
            dialog.Commands.Add(new UICommand("Cancel")); // Take no action
            await dialog.ShowAsync();
        }

        private void DoUnlink(IUICommand cmd)
        {
            _account.DeactivateAuthenticator(async response =>
            {
                if (response)
                {
                    _account.FullyEnrolled = false;
                    Storage.PushStore(_account);
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        SteamGuardButton_Click(null, null);
                    });
                }
                else
                {
                    var dialog = new MessageDialog("Failed to unlink authenticator") { Title = "Error" };
                    dialog.Commands.Add(new UICommand("Ok"));
                    await dialog.ShowAsync();
                }
            });
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            Storage.Logout();
            Frame.Navigate(typeof(LoginPage));
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(UsersPage));
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AboutPage));
        }
    }
}
