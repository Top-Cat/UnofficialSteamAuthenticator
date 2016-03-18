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

            BtnContinue.Visibility = SmsGrid.Visibility = PhoneNumGrid.Visibility = RevocationGrid.Visibility = ErrorLabel.Visibility = Visibility.Collapsed;
            Progress.Visibility = Visibility.Visible;

            _linker = new AuthenticatorLinker(Storage.GetSessionData())
            {
                LinkedAccount = Storage.GetSteamGuardAccount()
            };
            _linker.AddAuthenticator(LinkResponse);
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            PhoneNum.IsTabStop = SmsCode.IsTabStop = false;
            ErrorLabel.Visibility = BtnContinue.Visibility = Visibility.Collapsed;
            Progress.Visibility = Visibility.Visible;
            PhoneNum.IsTabStop = SmsCode.IsTabStop = true;

            if (PhoneNumGrid.Visibility == Visibility.Visible)
            {
                _linker.PhoneNumber = FilterPhoneNumber(PhoneNum.Text);
                _linker.AddAuthenticator(LinkResponse);
            }
            else if (RevocationGrid.Visibility == Visibility.Visible)
            {
                SmsCode.Text = string.Empty;
                Progress.Visibility = RevocationGrid.Visibility = Visibility.Collapsed;
                BtnContinue.Visibility = SmsGrid.Visibility = Visibility.Visible;
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
                ErrorLabel.Text = StringResourceLoader.GetString("BadCode");
                SmsCode.Text = string.Empty;
                Progress.Visibility = Visibility.Collapsed;
                BtnContinue.Visibility = ErrorLabel.Visibility = Visibility.Visible;
            }
            else if (response == AuthenticatorLinker.FinalizeResult.UnableToGenerateCorrectCodes || response == AuthenticatorLinker.FinalizeResult.GeneralFailure)
            {
                // Go back to main app screen on unknown failure

                var dialog = new MessageDialog(StringResourceLoader.GetString("Authenticator_Link_UnknownError_Message"))
                {
                    Title = StringResourceLoader.GetString("Authenticator_Link_UnknownError_Title")
                };
                dialog.Commands.Add(new UICommand(StringResourceLoader.GetString("UiCommand_Ok_Text")));
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
                ErrorLabel.Text = StringResourceLoader.GetString("EnterPhoneNumber");
                if (!firstRun)
                {
                    ErrorLabel.Visibility = Visibility.Visible;
                }
                PhoneNum.Text = string.Empty;
                PhoneNumGrid.Visibility = Visibility.Visible;
            }
            else if (linkResponse == AuthenticatorLinker.LinkResult.MustRemovePhoneNumber)
            {
                PhoneNum.Text = string.Empty;
                _linker.PhoneNumber = string.Empty;
                _linker.AddAuthenticator(LinkResponse);
            }
            else if (linkResponse == AuthenticatorLinker.LinkResult.GeneralFailure || linkResponse == AuthenticatorLinker.LinkResult.AuthenticatorPresent)
            {
                // Possibly because of rate limiting etc, force user to start process again manually

                var dialog = new MessageDialog(linkResponse == AuthenticatorLinker.LinkResult.GeneralFailure
                    ? StringResourceLoader.GetString("Authenticator_Link_UnknownError_Message")
                    : StringResourceLoader.GetString("Authenticator_Link_AlreadyLinked_Message"))
                {
                    Title = StringResourceLoader.GetString("Authenticator_Link_UnknownError_Title")
                };
                dialog.Commands.Add(new UICommand(StringResourceLoader.GetString("UiCommand_Ok_Text")));
                await dialog.ShowAsync();

                Frame.Navigate(typeof(MainPage));
            }
            else if (linkResponse == AuthenticatorLinker.LinkResult.AwaitingFinalization)
            {
                Storage.PushStore(_linker.LinkedAccount);

                RevocationGrid.Visibility = Visibility.Visible;
                RevocationCode.Text = _linker.LinkedAccount.RevocationCode;
            }

            BtnContinue.Visibility = Visibility.Visible;
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
            return phoneNumber.Replace("-", string.Empty).Replace("(", string.Empty).Replace(")", string.Empty);
        }
    }
}
