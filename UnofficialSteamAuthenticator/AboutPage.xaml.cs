using System;
using Windows.Phone.UI.Input;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace UnofficialSteamAuthenticator
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
            this.SourceRow.PointerPressed += this.SourceRow_PointerPressed;
        }

        private async void SourceRow_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var github = new Uri("http://github.com/Top-Cat/UnofficialSteamAuthenticator");
            await Launcher.LaunchUriAsync(github);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += this.BackPressed;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= this.BackPressed;
        }

        private void BackPressed(object sender, BackPressedEventArgs args)
        {
            if (this.Frame.CanGoBack)
            {
                args.Handled = true;
                this.Frame.GoBack();
            }
        }
    }
}
