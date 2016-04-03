using System;
using System.Collections.Generic;
using Windows.Phone.UI.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using UnofficialSteamAuthenticator.Lib.SteamAuth;
using UnofficialSteamAuthenticator.Lib;

namespace UnofficialSteamAuthenticator
{
    public sealed partial class LoginPage
    {
        private readonly Dictionary<LoginResult, string> responses = new Dictionary<LoginResult, string>();
        private readonly SteamWeb web = ((App) Application.Current).SteamWeb;
        private UserLogin userLogin;

        public LoginPage()
        {
            this.InitializeComponent();

            this.responses.Add(LoginResult.GeneralFailure, StringResourceLoader.GetString("LoginResult_GeneralFailure_Text"));
            this.responses.Add(LoginResult.BadRsa, StringResourceLoader.GetString("LoginResult_BadRSA_Text"));
            this.responses.Add(LoginResult.BadCredentials, StringResourceLoader.GetString("LoginResult_BadCredentials_Text"));
            this.responses.Add(LoginResult.TooManyFailedLogins, StringResourceLoader.GetString("LoginResult_TooManyFailedLogins_Text"));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += this.BackPressed;
            this.ResetView();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= this.BackPressed;
        }

        private void BackPressed(object s, BackPressedEventArgs args)
        {
            if (this.LoginGrid.Visibility == Visibility.Collapsed)
            {
                args.Handled = true;
                this.ResetView();
            }
            else if (this.Frame.CanGoBack)
            {
                args.Handled = true;
                this.Frame.GoBack();
            }
        }

        private void ResetView()
        {
            this.HideAll();
            this.ErrorLabel.Visibility = Visibility.Collapsed;
            this.LoginBtn.Visibility = this.LoginGrid.Visibility = Visibility.Visible;
            this.UserName.Text = this.PasswordBox.Password = string.Empty;
            this.AppBar.Visibility = Storage.GetAccounts().Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            this.UserName.IsTabStop = this.PasswordBox.IsTabStop = this.CaptchaText.IsTabStop = this.EmailCode.IsTabStop = false;
            this.ErrorLabel.Visibility = this.LoginBtn.Visibility = Visibility.Collapsed;
            this.Progress.Visibility = Visibility.Visible;
            this.UserName.IsTabStop = this.PasswordBox.IsTabStop = this.CaptchaText.IsTabStop = this.EmailCode.IsTabStop = true;

            if (this.LoginGrid.Visibility == Visibility.Visible)
            {
                this.userLogin = new UserLogin(this.UserName.Text, this.PasswordBox.Password);
            }

            this.userLogin.TwoFactorCode = this.TwoFactorCode.Text.ToUpper();
            this.userLogin.EmailCode = this.EmailCode.Text.ToUpper();
            this.userLogin.CaptchaText = this.CaptchaText.Text;

            this.userLogin.DoLogin(this.web, async response =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.ProcessLoginResponse(response);
                });
            });
        }

        private void ProcessLoginResponse(LoginResult response)
        {
            this.CaptchaText.Text = string.Empty;
            this.HideAll();
            this.Progress.IsEnabled = false;
            this.LoginBtn.Visibility = Visibility.Visible;

            if (response == LoginResult.NeedEmail)
            {
                this.ErrorLabel.Text = StringResourceLoader.GetString("Need2Fa");
                this.EmailCode.Text = string.Empty;
                this.EmailGrid.Visibility = this.ErrorLabel.Visibility = Visibility.Visible;
            }
            else if (this.responses.ContainsKey(response))
            {
                this.ErrorLabel.Text = this.responses[response];
                this.LoginGrid.Visibility = this.ErrorLabel.Visibility = Visibility.Visible;
            }
            else if (response == LoginResult.Need2Fa)
            {
                SteamGuardAccount account = Storage.GetSteamGuardAccount(this.UserName.Text);
                if (string.IsNullOrWhiteSpace(this.userLogin.TwoFactorCode) && account != null)
                {
                    this.Progress.Visibility = this.LoginGrid.Visibility = Visibility.Visible;
                    this.LoginBtn.Visibility = Visibility.Collapsed;

                    account.GenerateSteamGuardCode(this.web, async code =>
                    {
                        if (string.IsNullOrWhiteSpace(code))
                        {
                            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                this.userLogin.TwoFactorCode = "-";
                                this.ProcessLoginResponse(LoginResult.Need2Fa);
                            });
                            return;
                        }

                        this.userLogin.TwoFactorCode = code;

                        this.userLogin.DoLogin(this.web, async res =>
                        {
                            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                this.ProcessLoginResponse(res);
                            });
                        });
                    });
                }
                else
                {
                    this.ErrorLabel.Text = StringResourceLoader.GetString("Need2Fa");
                    this.TwoFactorCode.Text = string.Empty;
                    this.TwoFactorGrid.Visibility = this.ErrorLabel.Visibility = Visibility.Visible;
                }
            }
            else if (response == LoginResult.NeedCaptcha)
            {
                this.ErrorLabel.Text = StringResourceLoader.GetString("AreYouHuman");
                this.CaptchaText.Text = string.Empty;
                this.CaptchaGrid.Visibility = this.ErrorLabel.Visibility = Visibility.Visible;

                var myUri = new Uri("https://steamcommunity.com/login/rendercaptcha/?gid=" + this.userLogin.CaptchaGID, UriKind.Absolute);
                var bmi = new BitmapImage()
                {
                    CreateOptions = BitmapCreateOptions.IgnoreImageCache,
                    UriSource = myUri
                };
                this.Captcha.Source = bmi;
            }
            else if (response == LoginResult.LoginOkay)
            {
                Storage.PushStore(this.userLogin.Session);
                Storage.SetCurrentUser(this.userLogin.Session.SteamID);
                this.Frame.Navigate(typeof(MainPage), this.userLogin.Session);
            }
        }

        // I'm going to hell <- yeah, you are :D
        private void HideAll()
        {
            this.AppBar.Visibility = this.TwoFactorGrid.Visibility = this.EmailGrid.Visibility = this.Progress.Visibility = this.LoginGrid.Visibility = this.CaptchaGrid.Visibility = Visibility.Collapsed;
        }

        private void SwitchUser_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(UsersPage));
        }
    }
}
