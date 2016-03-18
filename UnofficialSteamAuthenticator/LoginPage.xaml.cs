using System;
using System.Collections.Generic;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

using SteamAuth;
using Windows.UI.Xaml.Media.Imaging;

namespace UnofficialSteamAuthenticator
{
    public sealed partial class LoginPage
    {
        private UserLogin _userLogin;
        private readonly Dictionary<LoginResult, string> _responses = new Dictionary<LoginResult, string>();

        public LoginPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;

            _responses.Add(LoginResult.GeneralFailure, "Unknown Error");
            _responses.Add(LoginResult.BadRSA, "Unknown Error");
            _responses.Add(LoginResult.BadCredentials, "Invalid Login");
            _responses.Add(LoginResult.TooManyFailedLogins, "Too many failures");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += BackPressed;
            ResetView();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= BackPressed;
        }

        private void BackPressed(object s, BackPressedEventArgs args)
        {
            if (LoginGrid.Visibility == Visibility.Collapsed)
            {
                args.Handled = true;
                ResetView();
            }
            else if (Frame.CanGoBack)
            {
                args.Handled = true;
                Frame.GoBack();
            }
        }

        private void ResetView()
        {
            HideAll();
            ErrorLabel.Visibility = Visibility.Collapsed;
            LoginBtn.Visibility = LoginGrid.Visibility = Visibility.Visible;
            UserName.Text = PasswordBox.Password = string.Empty;
            AppBar.Visibility = Storage.GetAccounts().Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            UserName.IsTabStop = PasswordBox.IsTabStop = false;
            ErrorLabel.Visibility = LoginBtn.Visibility = Visibility.Collapsed;
            Progress.Visibility = Visibility.Visible;
            UserName.IsTabStop = PasswordBox.IsTabStop = true;

            if (_userLogin == null || _userLogin.Username != UserName.Text)
            {
                _userLogin = new UserLogin(UserName.Text, PasswordBox.Password);
            }

            _userLogin.TwoFactorCode = TwoFactorCode.Text.ToUpper();
            _userLogin.EmailCode = EmailCode.Text.ToUpper();
            _userLogin.CaptchaText = CaptchaText.Text;

            _userLogin.DoLogin(async response =>
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ProcessLoginResponse(response);
                });
            });
        }

        private void ProcessLoginResponse(LoginResult response)
        {
            CaptchaText.Text = string.Empty;
            HideAll();
            Progress.IsEnabled = false;
            LoginBtn.Visibility = Visibility.Visible;

            if (response == LoginResult.NeedEmail)
            {
                ErrorLabel.Text = "Need 2FA";
                EmailCode.Text = string.Empty;
                EmailGrid.Visibility = ErrorLabel.Visibility = Visibility.Visible;
            }
            else if (_responses.ContainsKey(response))
            {
                ErrorLabel.Text = _responses[response];
                LoginGrid.Visibility = ErrorLabel.Visibility = Visibility.Visible;
            }
            else if (response == LoginResult.Need2FA)
            {
                SteamGuardAccount account = Storage.GetSteamGuardAccount(UserName.Text);
                if (string.IsNullOrWhiteSpace(_userLogin.TwoFactorCode) && account != null)
                {
                    Progress.Visibility = LoginGrid.Visibility = Visibility.Visible;
                    LoginBtn.Visibility = Visibility.Collapsed;

                    account.GenerateSteamGuardCode(async code =>
                    {
                        if (string.IsNullOrWhiteSpace(code))
                        {
                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                _userLogin.TwoFactorCode = " ";
                                ProcessLoginResponse(LoginResult.Need2FA);
                            });
                            return;
                        }

                        _userLogin.TwoFactorCode = code;

                        _userLogin.DoLogin(async res =>
                        {
                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                ProcessLoginResponse(res);
                            });
                        });
                    });
                }
                else
                {
                    ErrorLabel.Text = "Need 2FA";
                    TwoFactorCode.Text = string.Empty;
                    TwoFactorGrid.Visibility = ErrorLabel.Visibility = Visibility.Visible;
                }
            }
            else if (response == LoginResult.NeedCaptcha)
            {
                ErrorLabel.Text = "Are you human?";
                CaptchaText.Text = string.Empty;
                CaptchaGrid.Visibility = ErrorLabel.Visibility = Visibility.Visible;

                Uri myUri = new Uri("https://steamcommunity.com/login/rendercaptcha/?gid=" + _userLogin.CaptchaGID, UriKind.Absolute);
                BitmapImage bmi = new BitmapImage();
                bmi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmi.UriSource = myUri;
                Captcha.Source = bmi;
            }
            else if (response == LoginResult.LoginOkay)
            {
                Storage.PushStore(_userLogin.Session);
                Frame.Navigate(typeof(MainPage), _userLogin.Session);
            }
        }

        // I'm going to hell
        private void HideAll()
        {
            AppBar.Visibility = TwoFactorGrid.Visibility = EmailGrid.Visibility = Progress.Visibility = LoginGrid.Visibility = CaptchaGrid.Visibility = Visibility.Collapsed;
        }

        private void SwitchUser_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(UsersPage));
        }
    }
}
