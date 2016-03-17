using System;
using Windows.Phone.UI.Input;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UnofficialSteamAuthenticator
{

    public sealed partial class About : Page
    {
        public About()
        {
            this.InitializeComponent();
            sourceRow.PointerPressed += SourceRow_PointerPressed;
        }

        private async void SourceRow_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Uri github = new Uri("http://github.com/Top-Cat/UnofficialSteamAuthenticator");
            await Launcher.LaunchUriAsync(github);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += BackPressed;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= BackPressed;
        }

        private void BackPressed(object sender, BackPressedEventArgs args)
        {
            if (Frame.CanGoBack) {
                args.Handled = true;
                Frame.GoBack();
            }
        }
    }
}
