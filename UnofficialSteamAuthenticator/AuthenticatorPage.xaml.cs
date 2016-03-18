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
        private AuthenticatorLinker linker;

        public AuthenticatorPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= this.NavigateBack;
        }

        private void NavigateBack(object s, BackPressedEventArgs args)
        {
            args.Handled = true;
            this.Frame.Navigate(typeof(MainPage));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += this.NavigateBack;

            this.BtnContinue.Visibility = this.SmsGrid.Visibility = this.PhoneNumGrid.Visibility = this.RevocationGrid.Visibility = this.ErrorLabel.Visibility = Visibility.Collapsed;
            this.Progress.Visibility = Visibility.Visible;

            this.linker = new AuthenticatorLinker(Storage.GetSessionData())
            {
                LinkedAccount = Storage.GetSteamGuardAccount()
            };
            this.linker.AddAuthenticator(this.LinkResponse);
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            this.PhoneNum.IsTabStop = this.SmsCode.IsTabStop = false;
            this.ErrorLabel.Visibility = this.BtnContinue.Visibility = Visibility.Collapsed;
            this.Progress.Visibility = Visibility.Visible;
            this.PhoneNum.IsTabStop = this.SmsCode.IsTabStop = true;

            if (this.PhoneNumGrid.Visibility == Visibility.Visible)
            {
                this.linker.PhoneNumber = this.FilterPhoneNumber(this.PhoneNum.Text);
                this.linker.AddAuthenticator(this.LinkResponse);
            }
            else if (this.RevocationGrid.Visibility == Visibility.Visible)
            {
                this.SmsCode.Text = string.Empty;
                this.Progress.Visibility = this.RevocationGrid.Visibility = Visibility.Collapsed;
                this.BtnContinue.Visibility = this.SmsGrid.Visibility = Visibility.Visible;
            }
            else if (this.SmsGrid.Visibility == Visibility.Visible)
            {
                this.linker.FinalizeAddAuthenticator(async response =>
                {
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        this.FinaliseResponse(response);
                    });
                }, this.SmsCode.Text);
            }
        }

        private async void FinaliseResponse(AuthenticatorLinker.FinalizeResult response)
        {
            if (response == AuthenticatorLinker.FinalizeResult.BadSMSCode)
            {
                this.ErrorLabel.Text = StringResourceLoader.GetString("BadCode");
                this.SmsCode.Text = string.Empty;
                this.Progress.Visibility = Visibility.Collapsed;
                this.BtnContinue.Visibility = this.ErrorLabel.Visibility = Visibility.Visible;
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

                this.Frame.Navigate(typeof(MainPage));
            }
            else if (response == AuthenticatorLinker.FinalizeResult.Success)
            {
                Storage.PushStore(this.linker.LinkedAccount);
                this.Frame.Navigate(typeof(MainPage));
            }
        }

        private async void LinkResponseReal(AuthenticatorLinker.LinkResult linkResponse)
        {
            bool firstRun = this.PhoneNumGrid.Visibility == Visibility.Collapsed;
            this.Progress.Visibility = this.ErrorLabel.Visibility = this.SmsGrid.Visibility = this.PhoneNumGrid.Visibility = this.RevocationGrid.Visibility = Visibility.Collapsed;

            if (linkResponse == AuthenticatorLinker.LinkResult.MustProvidePhoneNumber)
            {
                this.ErrorLabel.Text = StringResourceLoader.GetString("EnterPhoneNumber");
                if (!firstRun)
                {
                    this.ErrorLabel.Visibility = Visibility.Visible;
                }
                this.PhoneNum.Text = string.Empty;
                this.PhoneNumGrid.Visibility = Visibility.Visible;
            }
            else if (linkResponse == AuthenticatorLinker.LinkResult.MustRemovePhoneNumber)
            {
                this.PhoneNum.Text = string.Empty;
                this.linker.PhoneNumber = string.Empty;
                this.linker.AddAuthenticator(this.LinkResponse);
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

                this.Frame.Navigate(typeof(MainPage));
            }
            else if (linkResponse == AuthenticatorLinker.LinkResult.AwaitingFinalization)
            {
                Storage.PushStore(this.linker.LinkedAccount);

                this.RevocationGrid.Visibility = Visibility.Visible;
                this.RevocationCode.Text = this.linker.LinkedAccount.RevocationCode;
            }

            this.BtnContinue.Visibility = Visibility.Visible;
        }

        private async void LinkResponse(AuthenticatorLinker.LinkResult linkResponse)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.LinkResponseReal(linkResponse);
            });
        }

        public string FilterPhoneNumber(string phoneNumber)
        {
            return phoneNumber.Replace("-", string.Empty).Replace("(", string.Empty).Replace(")", string.Empty);
        }
    }
}
