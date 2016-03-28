using System;
using Windows.Phone.UI.Input;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using UnofficialSteamAuthenticator.SteamAuth;
using Windows.UI.Xaml.Controls;

namespace UnofficialSteamAuthenticator
{
    public sealed partial class AuthenticatorPage : Page
    {
        private readonly SteamWeb web = ((App) Application.Current).SteamWeb;
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
            this.linker.AddAuthenticator(this.web, this.LinkResponse);
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
                this.linker.AddAuthenticator(this.web, this.LinkResponse);
            }
            else if (this.RevocationGrid.Visibility == Visibility.Visible)
            {
                this.SmsCode.Text = string.Empty;
                this.Progress.Visibility = this.RevocationGrid.Visibility = Visibility.Collapsed;
                this.BtnContinue.Visibility = this.SmsGrid.Visibility = Visibility.Visible;
            }
            else if (this.SmsGrid.Visibility == Visibility.Visible)
            {
                this.linker.FinalizeAddAuthenticator(this.web, this.SmsCode.Text, async response =>
                {
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        this.FinaliseResponse(response);
                    });
                });
            }
        }

        private async void FinaliseResponse(AuthenticatorLinker.FinalizeResult response)
        {
            switch (response)
            {
                case AuthenticatorLinker.FinalizeResult.BadSMSCode:
                    this.ErrorLabel.Text = StringResourceLoader.GetString("BadCode");
                    this.SmsCode.Text = string.Empty;
                    this.Progress.Visibility = Visibility.Collapsed;
                    this.BtnContinue.Visibility = this.ErrorLabel.Visibility = Visibility.Visible;
                    break;
                case AuthenticatorLinker.FinalizeResult.UnableToGenerateCorrectCodes:
                case AuthenticatorLinker.FinalizeResult.GeneralFailure:
                    // Go back to main app screen on unknown failure

                    var dialog = new MessageDialog(StringResourceLoader.GetString("Authenticator_Link_UnknownError_Message"))
                    {
                        Title = StringResourceLoader.GetString("Authenticator_Link_UnknownError_Title")
                    };
                    dialog.Commands.Add(new UICommand(StringResourceLoader.GetString("UiCommand_Ok_Text")));
                    await dialog.ShowAsync();

                    this.Frame.Navigate(typeof(MainPage));
                    break;
                case AuthenticatorLinker.FinalizeResult.Success:
                    this.linker.LinkedAccount.PushStore();
                    this.Frame.Navigate(typeof(MainPage));
                    break;
            }
        }

        private async void LinkResponseReal(AuthenticatorLinker.LinkResult linkResponse)
        {
            bool firstRun = this.PhoneNumGrid.Visibility == Visibility.Collapsed;
            this.Progress.Visibility = this.ErrorLabel.Visibility = this.SmsGrid.Visibility = this.PhoneNumGrid.Visibility = this.RevocationGrid.Visibility = Visibility.Collapsed;

            switch (linkResponse)
            {
                case AuthenticatorLinker.LinkResult.MustProvidePhoneNumber:
                    this.ErrorLabel.Text = StringResourceLoader.GetString("EnterPhoneNumber");
                    if (!firstRun)
                    {
                        this.ErrorLabel.Visibility = Visibility.Visible;
                    }
                    this.PhoneNum.Text = string.Empty;
                    this.PhoneNumGrid.Visibility = Visibility.Visible;
                    break;
                case AuthenticatorLinker.LinkResult.MustRemovePhoneNumber:
                    this.PhoneNum.Text = string.Empty;
                    this.linker.PhoneNumber = string.Empty;
                    this.linker.AddAuthenticator(this.web, this.LinkResponse);
                    break;
                case AuthenticatorLinker.LinkResult.GeneralFailure:
                case AuthenticatorLinker.LinkResult.AuthenticatorPresent:
                    // Possibly because of rate limiting etc, force user to start process again manually

                    var dialog = new MessageDialog(linkResponse == AuthenticatorLinker.LinkResult.GeneralFailure ? StringResourceLoader.GetString("Authenticator_Link_UnknownError_Message") : StringResourceLoader.GetString("Authenticator_Link_AlreadyLinked_Message"))
                    {
                        Title = StringResourceLoader.GetString("Authenticator_Link_UnknownError_Title")
                    };
                    dialog.Commands.Add(new UICommand(StringResourceLoader.GetString("UiCommand_Ok_Text")));
                    await dialog.ShowAsync();

                    this.Frame.Navigate(typeof(MainPage));
                    break;
                case AuthenticatorLinker.LinkResult.AwaitingFinalization:
                    this.linker.LinkedAccount.PushStore();

                    this.RevocationGrid.Visibility = Visibility.Visible;
                    this.RevocationCode.Text = this.linker.LinkedAccount.RevocationCode;
                    break;
            }

            this.BtnContinue.Visibility = Visibility.Visible;
        }

        private async void LinkResponse(AuthenticatorLinker.LinkResult linkResponse)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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
