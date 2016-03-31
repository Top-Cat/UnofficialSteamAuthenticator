using System;
using System.Globalization;
using Windows.ApplicationModel.Resources.Core;
using Windows.Globalization;
using Windows.Phone.UI.Input;
using Windows.System;
using Windows.System.UserProfile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UnofficialSteamAuthenticator
{
    public sealed partial class SettingsPage
    {
        public SettingsPage()
        {
            this.InitializeComponent();

            string currentLanguage = ResourceContext.GetForCurrentView().Languages[0];
            foreach (string language in GlobalizationPreferences.Languages)
            {
                var model = new Lib.Models.Language(language);
                this.LanguageCombo.Items?.Add(model);

                if (language == currentLanguage)
                {
                    this.LanguageCombo.SelectedItem = model;
                }
            }

            this.LanguageCombo.SelectionChanged += LanguageCombo_OnSelectionChanged;
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

        private static void LanguageCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var language = (Lib.Models.Language) e.AddedItems[0];
            ApplicationLanguages.PrimaryLanguageOverride = language.Code;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }
    }
}
