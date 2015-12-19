using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using SteamAuth;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;

namespace SteamAppNative
{
    public sealed partial class Login : Page
    {
        private UserLogin login;
        private Dictionary<LoginResult, string> responses = new Dictionary<LoginResult, string>();

        public Login()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;

            responses.Add(LoginResult.GeneralFailure, "Unknown Error");
            responses.Add(LoginResult.BadRSA, "Unknown Error");
            responses.Add(LoginResult.BadCredentials, "Invalid Login");
            responses.Add(LoginResult.TooManyFailedLogins, "Too many failures");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += BackPressed;
            ResetView();
        }

        private void BackPressed(object s, BackPressedEventArgs args)
        {
            if (LoginGrid.Visibility == Visibility.Collapsed)
            {
                ResetView();
                args.Handled = true;
            }
        }

        private void ResetView()
        {
            HideAll();
            ErrorLabel.Visibility = Visibility.Collapsed;
            LoginGrid.Visibility = Visibility.Visible;
            UserName.Text = PassWord.Password = "";
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            UserName.IsTabStop = PassWord.IsTabStop = false;
            ErrorLabel.Visibility = LoginBtn.Visibility = Visibility.Collapsed;
            Progress.IsEnabled = true;
            Progress.Visibility = Visibility.Visible;
            UserName.IsTabStop = PassWord.IsTabStop = true;

            if (login == null || login.Username != UserName.Text)
            {
                login = new UserLogin(UserName.Text, PassWord.Password);
            }

            login.TwoFactorCode = TwoFactorCode.Text;
            login.EmailCode = EmailCode.Text;
            login.CaptchaText = CaptchaText.Text;

            login.DoLogin(response =>
            {
                Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ProcessLoginResponse(response);
                });
            });
        }

        private void ProcessLoginResponse(LoginResult response)
        {
            CaptchaText.Text = "";
            HideAll();
            Progress.IsEnabled = false;
            LoginBtn.Visibility = Visibility.Visible;

            if (response == LoginResult.NeedEmail)
            {
                ErrorLabel.Text = "Need 2FA";
                EmailCode.Text = "";
                EmailGrid.Visibility = ErrorLabel.Visibility = Visibility.Visible;
            }
            else if (responses.ContainsKey(response))
            {
                ErrorLabel.Text = responses[response];
                LoginGrid.Visibility = ErrorLabel.Visibility = Visibility.Visible;
            } 
            else if (response == LoginResult.Need2FA)
            {
                Progress.IsEnabled = true;
                SteamGuardAccount account = Storage.SGAFromStore(UserName.Text);
                if (login.TwoFactorCode == null && account != null)
                {
                    LoginGrid.Visibility = Visibility.Visible;
                    account.GenerateSteamGuardCode(code =>
                    {
                        login.TwoFactorCode = code;

                        login.DoLogin(res =>
                        {
                            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                ProcessLoginResponse(res);
                            });
                        });
                    });
                }
                else
                {
                    ErrorLabel.Text = "Need 2FA";
                    TwoFactorCode.Text = "";
                    TwoFactorGrid.Visibility = ErrorLabel.Visibility = Visibility.Visible;
                }
            }
            else if (response == LoginResult.NeedCaptcha)
            {
                ErrorLabel.Text = "Are you human?";
                CaptchaText.Text = "";
                CaptchaGrid.Visibility = ErrorLabel.Visibility = Visibility.Visible;

                Uri myUri = new Uri("https://steamcommunity.com/login/rendercaptcha/?gid=" + login.CaptchaGID, UriKind.Absolute);
                BitmapImage bmi = new BitmapImage();
                bmi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmi.UriSource = myUri;
                Captcha.Source = bmi;
            }
            else if (response == LoginResult.LoginOkay)
            {
                Storage.PushStore(login.Session);
                Frame.Navigate(typeof(MainPage), login.Session);
            }
        }

        // I'm going to hell
        private void HideAll()
        {
            TwoFactorGrid.Visibility = EmailGrid.Visibility = Progress.Visibility = LoginGrid.Visibility = CaptchaGrid.Visibility = Visibility.Collapsed;
        }
    }
}
