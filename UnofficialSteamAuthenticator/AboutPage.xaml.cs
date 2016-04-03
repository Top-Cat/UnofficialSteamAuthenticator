using System;
using Windows.ApplicationModel;
using Windows.Phone.UI.Input;
using Windows.System;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace UnofficialSteamAuthenticator
{
    public sealed partial class AboutPage
    {
        public AboutPage()
        {
            this.InitializeComponent();
            this.SourceRow.PointerPressed += SourceRow_PointerPressed;
            this.About_AppVersionTxt.Text = $"v{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";
        }

        private static async void SourceRow_PointerPressed(object sender, PointerRoutedEventArgs e)
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
            if (!this.Frame.CanGoBack)
                return;

            args.Handled = true;
            this.Frame.GoBack();
        }
    }
}
