using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Phone.UI.Input;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.Web;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using UnofficalSteamAuthenticator.NotificationTask;
using UnofficialSteamAuthenticator.Lib.SteamAuth;
using UnofficialSteamAuthenticator.Lib.Models;
using UnofficialSteamAuthenticator.Lib;

namespace UnofficialSteamAuthenticator
{
    public sealed partial class MainPage
    {
        private const string ChatUrl = "https://steamcommunity.com/chat";
        private readonly SteamWeb web = ((App) Application.Current).SteamWeb;
        private SteamGuardAccount account;
        private string confUrl = string.Empty;
        private string confWebUrl = string.Empty;
        private Storyboard storyboard;
        private readonly Regex confIdRegex = new Regex("#conf_(\\d+)");

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void WebNotify(object sender, NotifyEventArgs e)
        {
            this.HandleUri(new Uri(e.Value));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.ConfirmationWeb.ScriptNotify += this.WebNotify;
            this.ConfirmationWeb.NavigationCompleted += this.InjectCode;
            this.ConfirmationWeb.NavigationFailed += this.NavFailed;
            this.ChatWeb.NavigationFailed += this.NavFailed;
            HardwareButtons.BackPressed += this.BackPressed;

            if (e.Parameter is string)
            {
                var steamid = ulong.Parse((string) e.Parameter);
                Storage.SetCurrentUser(steamid);

                this.account = Storage.GetSteamGuardAccount();
                this.ConfirmationsButton_Click(null, null);
            }
            else
            {
                this.account = Storage.GetSteamGuardAccount();
                this.SteamGuardButton_Click(null, null);
            }

            if (this.account != null && (DateTime.UtcNow - this.account.DisplayCache).Days > 1)
            {
                UserSummary.GetSummaries(this.web, this.account.Session, new [] { this.account.Session.SteamID }, this.SummariesCallback);
            }
        }

        private void NavFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            // Family view now returns 403
            if (e.WebErrorStatus == WebErrorStatus.Forbidden)
                return;

            Action<object, RoutedEventArgs> call = e.Uri.Fragment.Length > 0 ?
                (this.ConfirmationWeb.Visibility == Visibility.Visible ? (Action<object, RoutedEventArgs>) this.ConfirmationsButton_Click : this.MessageButton_Click) :
                this.SteamGuardButton_Click;

            call(null, null);
        }

        private async void SummariesCallback(Players responseObj)
        {
            if (responseObj?.PlayersList == null)
            {
                return;
            }

            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (Player p in responseObj.PlayersList.Where(p => p.SteamId == this.account.Session.SteamID))
                {
                    this.account.DisplayName = p.Username;
                    this.account.PushStore();

                    this.SteamGuardButton_Click(null, null);
                }
            });
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.ConfirmationWeb.ScriptNotify -= this.WebNotify;
            this.ConfirmationWeb.NavigationCompleted -= this.InjectCode;
            this.ConfirmationWeb.NavigationFailed += this.NavFailed;
            this.ChatWeb.NavigationFailed += this.NavFailed;
            HardwareButtons.BackPressed -= this.BackPressed;
        }

        private void BackPressed(object s, BackPressedEventArgs args)
        {
            if (this.ConfirmationWeb.Visibility == Visibility.Visible && this.ConfirmationWeb.CanGoBack && this.confWebUrl != this.confUrl)
            {
                this.ConfirmationWeb.GoBack();
                args.Handled = true;
            }
        }

        internal void HandleUri(Uri uri)
        {
            var query = new Dictionary<string, string>();
            if (uri.Query.Length > 0)
            {
                var decoder = new WwwFormUrlDecoder(uri.Query);
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
                    this.account.RefreshSession(this.web, async success =>
                    {
                        await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            if (success)
                            {
                                this.SteamGuardButton_Click(null, null);
                            }
                            else
                            {
                                Storage.Logout();
                                this.Frame.Navigate(typeof(LoginPage));
                            }
                        });
                    });
                    break;
                case "steamguard":
                    if (query["op"] != "conftag")
                    {
                        break;
                    }

                    this.account.GenerateConfirmationQueryParams(this.web, query["arg1"], async response =>
                    {
                        try
                        {
                            string[] args =
                            {
                                "window.SGHandler.update('" + response + "', 'ok');"
                            };
                            await this.ConfirmationWeb.InvokeScriptAsync("eval", args);
                        }
                        catch (Exception)
                        {
                            // We're probably here because the webview was unloaded
                            // Just reload the view
                            this.ConfirmationsButton_Click(null, null);
                        }
                    });

                    break;
                default:
                    Debug.WriteLine("Unhandled uri: " + uri.AbsoluteUri);
                    break;
            }
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

            if (this.SteamGuardGrid.Visibility != Visibility.Visible)
            {
                return;
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

                    this.SteamGuardCode.Text = this.account.GenerateSteamGuardCodeForTime(time);
                });
            });
        }

        private void SteamGuardButton_Click(object sender, RoutedEventArgs e)
        {
            this.HideAll();
            if (this.account == null || !this.account.FullyEnrolled)
            {
                this.LinkGrid.Visibility = Visibility.Visible;
                return;
            }

            this.AccountText.Text = string.Format(
                StringResourceLoader.GetString("MainPage_AccountText_Format"),
                this.account.DisplayName ?? string.Empty,
                this.account.AccountName ?? this.account.Session.Username ?? string.Empty
            );

            this.SteamGuardGrid.Visibility = Visibility.Visible;
            this.steamGuardUpdate_Tick(null, null);
        }

        private void MessageButton_Click(object sender, RoutedEventArgs e)
        {
            this.HideAll();
            this.ChatWeb.Visibility = Visibility.Visible;

            this.ChatWeb.NavigateWithHttpRequestMessage(this.GetMessageForUrl(ChatUrl));
        }

        private void ConfirmationsButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.account == null || !this.account.FullyEnrolled)
            {
                return;
            }

            this.HideAll();

            this.ConfirmationWeb.Visibility = Visibility.Visible;

            this.account.GenerateConfirmationUrl(this.web, async response =>
            {
                this.confUrl = response;
                HttpRequestMessage message = this.GetMessageForUrl(response);

                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.ConfirmationWeb.NavigateWithHttpRequestMessage(message);
                });
            });
        }

        private HttpRequestMessage GetMessageForUrl(string url)
        {
            var cookies = new CookieContainer();
            this.account.Session.AddCookies(cookies);

            var baseUri = new Uri(url);
            var filter = new HttpBaseProtocolFilter();

            foreach (Cookie c in cookies.GetCookies(SteamWeb.uri))
            {
                var cookie = new HttpCookie(c.Name, c.Domain, c.Path)
                {
                    Value = c.Value
                };
                filter.CookieManager.SetCookie(cookie, false);
            }

            return new HttpRequestMessage(HttpMethod.Get, baseUri);
        }

        private async void InjectCode(object sender, WebViewNavigationCompletedEventArgs e)
        {
            this.confWebUrl = e.Uri.AbsoluteUri;

            if (e.IsSuccess && this.confIdRegex.IsMatch(e.Uri.Fragment))
            {
                // Viewing a confirmation {confId}, if fragment is empty the list is being viewed
                int confId = int.Parse(this.confIdRegex.Matches(e.Uri.Fragment)[0].Groups[1].Value);
                this.account.NotifySince = confId;
                this.account.PushStore();
            }

            if (!e.IsSuccess || this.confWebUrl != this.confUrl)
                return;

            BackgroundTask.RemoveUserNotifications(this.account.Session.SteamID);

            // Try to only inject this once
            string[] args =
            {
                @"
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
                        runLocalUrlO = typeof(runLocalUrl) !== 'undefined' ? runLocalUrl : function() {};
                        runLocalUrl = function(url) {
                            window.SGHandler.update(0, 'busy');
                            runLocalUrlO(url);
                        }
                    }"
            };
            await this.ConfirmationWeb.InvokeScriptAsync("eval", args);
        }

        private void HideAll()
        {
            this.SteamGuardGrid.Visibility = this.LinkGrid.Visibility = this.ConfirmationWeb.Visibility = this.ChatWeb.Visibility = Visibility.Collapsed;
        }

        private void LinkBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.account == null || !this.account.FullyEnrolled)
            {
                this.Frame.Navigate(typeof(AuthenticatorPage));
            }
        }

        private async void UnlinkBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MessageDialog(StringResourceLoader.GetString("Authenticator_Unlink_Prompt_Message"))
            {
                Title = StringResourceLoader.GetString("Authenticator_Unlink_Prompt_Title")
            };
            dialog.Commands.Add(new UICommand(StringResourceLoader.GetString("UiCommand_Ok_Text"), this.DoUnlink));
            dialog.Commands.Add(new UICommand(StringResourceLoader.GetString("UiCommand_Cancel_Text"))); // Take no action
            await dialog.ShowAsync();
        }

        private void DoUnlink(IUICommand command)
        {
            this.account.DeactivateAuthenticator(this.web, async response =>
            {
                if (response)
                {
                    this.account.FullyEnrolled = false;
                    this.account.PushStore();
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        this.SteamGuardButton_Click(null, null);
                    });
                }
                else
                {
                    var dialog = new MessageDialog(StringResourceLoader.GetString("Authenticator_Unlink_Failed_Message"))
                    {
                        Title = StringResourceLoader.GetString("Authenticator_Unlink_Failed_Title")
                    };
                    dialog.Commands.Add(new UICommand(StringResourceLoader.GetString("UiCommand_Ok_Text")));
                    await dialog.ShowAsync();
                }
            });
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(UsersPage));
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }
    }
}
