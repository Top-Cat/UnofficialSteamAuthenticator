using System;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

using SteamAuth;
using Windows.UI.Popups;

namespace UnofficialSteamAuthenticator
{
    public sealed partial class AuthenticatorPage
    {
        private AuthenticatorLinker _linker;

        public AuthenticatorPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= NavigateBack;
        }

        private void NavigateBack(object s, BackPressedEventArgs args)
        {
            args.Handled = true;
            Frame.Navigate(typeof(MainPage));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += NavigateBack;

            LoginBtn.Visibility = SmsGrid.Visibility = PhoneNumGrid.Visibility = RevocationGrid.Visibility = ErrorLabel.Visibility = Visibility.Collapsed;
            Progress.Visibility = Visibility.Visible;

            _linker = new AuthenticatorLinker(Storage.GetSessionData());
            _linker.LinkedAccount = Storage.GetSteamGuardAccount();
            _linker.AddAuthenticator(LinkResponse);
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            PhoneNum.IsTabStop = SmsCode.IsTabStop = false;
            ErrorLabel.Visibility = LoginBtn.Visibility = Visibility.Collapsed;
            Progress.Visibility = Visibility.Visible;
            PhoneNum.IsTabStop = SmsCode.IsTabStop = true;

            if (PhoneNumGrid.Visibility == Visibility.Visible)
            {
                _linker.PhoneNumber = FilterPhoneNumber(PhoneNum.Text);
                _linker.AddAuthenticator(LinkResponse);
            }
            else if (RevocationGrid.Visibility == Visibility.Visible)
            {
                SmsCode.Text = "";
                Progress.Visibility = RevocationGrid.Visibility = Visibility.Collapsed;
                LoginBtn.Visibility = SmsGrid.Visibility = Visibility.Visible;
            }
            else if (SmsGrid.Visibility == Visibility.Visible)
            {
                _linker.FinalizeAddAuthenticator(async response =>
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        FinaliseResponse(response);
                    });
                }, SmsCode.Text);
            }
            
        }

        private async void FinaliseResponse(AuthenticatorLinker.FinalizeResult response)
        {
            if (response == AuthenticatorLinker.FinalizeResult.BadSMSCode)
            {
                ErrorLabel.Text = "Bad Code";
                SmsCode.Text = "";
                Progress.Visibility = Visibility.Collapsed;
                LoginBtn.Visibility = ErrorLabel.Visibility = Visibility.Visible;
            }
            else if (response == AuthenticatorLinker.FinalizeResult.UnableToGenerateCorrectCodes || response == AuthenticatorLinker.FinalizeResult.GeneralFailure)
            {
                // Go back to main app screen on unknown failure

                var dialog = new MessageDialog("Unknown error linking authenticator");
                dialog.Title = "Error";
                dialog.Commands.Add(new UICommand("Ok"));
                await dialog.ShowAsync();

                Frame.Navigate(typeof(MainPage));
            }
            else if (response == AuthenticatorLinker.FinalizeResult.Success)
            {
                Storage.PushStore(_linker.LinkedAccount);
                Frame.Navigate(typeof(MainPage));
            }
        }

        private async void LinkResponseReal(AuthenticatorLinker.LinkResult linkResponse)
        {
            bool firstRun = PhoneNumGrid.Visibility == Visibility.Collapsed;
            Progress.Visibility = ErrorLabel.Visibility = SmsGrid.Visibility = PhoneNumGrid.Visibility = RevocationGrid.Visibility = Visibility.Collapsed;

            if (linkResponse == AuthenticatorLinker.LinkResult.MustProvidePhoneNumber)
            {
                ErrorLabel.Text = "Enter Phone Number";
                if (!firstRun)
                {
                    ErrorLabel.Visibility = Visibility.Visible;
                }
                PhoneNum.Text = "";
                PhoneNumGrid.Visibility = Visibility.Visible;
            }
            else if (linkResponse == AuthenticatorLinker.LinkResult.MustRemovePhoneNumber)
            {
                PhoneNum.Text = "";
                _linker.PhoneNumber = "";
                _linker.AddAuthenticator(LinkResponse);
            }
            else if (linkResponse == AuthenticatorLinker.LinkResult.GeneralFailure || linkResponse == AuthenticatorLinker.LinkResult.AuthenticatorPresent)
            {
                // Possibly because of rate limiting etc, force user to start process again manually

                var dialog = new MessageDialog(linkResponse == AuthenticatorLinker.LinkResult.GeneralFailure ?
                    "Unknown error linking authenticator" :
                    "You already have another device set up as an authenticator. Remove it from your account and try again.");
                dialog.Title = "Error";
                dialog.Commands.Add(new UICommand("Ok"));
                await dialog.ShowAsync();

                Frame.Navigate(typeof(MainPage));
            }
            else if (linkResponse == AuthenticatorLinker.LinkResult.AwaitingFinalization)
            {
                Storage.PushStore(_linker.LinkedAccount);

                RevocationGrid.Visibility = Visibility.Visible;
                RevocationCode.Text = _linker.LinkedAccount.RevocationCode;
            }

            LoginBtn.Visibility = Visibility.Visible;
        }

        private async void LinkResponse(AuthenticatorLinker.LinkResult linkResponse)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                LinkResponseReal(linkResponse);
            });
        }

        public string FilterPhoneNumber(string phoneNumber)
        {
            return phoneNumber.Replace("-", "").Replace("(", "").Replace(")", "");
        }
    }
}
