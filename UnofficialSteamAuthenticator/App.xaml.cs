using System;
using System.Diagnostics;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Microsoft.ApplicationInsights;
using UnofficialSteamAuthenticator.Models;
using UnofficialSteamAuthenticator.SteamAuth;

// The WebView Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641

namespace UnofficialSteamAuthenticator
{
    /// <summary>
    ///     Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        public SteamWeb SteamWeb = new SteamWeb();
        private TransitionCollection transitions;

        /// <summary>
        ///     Initializes the singleton application object.  This is the first line of authored code
        ///     executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            WindowsAppInitializer.InitializeAsync();
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            switch (args.Kind)
            {
                case ActivationKind.Protocol:
                    var protocolArgs = (ProtocolActivatedEventArgs) args;

                    var rootFrame = (Frame) Window.Current.Content;
                    var mainPage = (MainPage) rootFrame.Content;
                    if (mainPage != null)
                    {
                        mainPage.HandleUri(protocolArgs.Uri);
                    }
                    else
                    {
                        this.OpenApp(rootFrame, protocolArgs.Uri);
                    }
                    break;
                case ActivationKind.PickFolderContinuation:
                    var pickFolderArgs = (FolderPickerContinuationEventArgs) args;

                    if (pickFolderArgs.Folder == null) // Probably cancelled
                        return;

                    var usr = (ulong) pickFolderArgs.ContinuationData["user"];
                    SdaStorage.SaveMaFile(usr, pickFolderArgs.Folder);
                    break;
            }
        }

        /// <summary>
        ///     Invoked when the application is launched normally by the end user.  Other entry points
        ///     will be used when the application is launched to open a specific file, to display
        ///     search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            var rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // TODO: change this value to a cache size that is appropriate for your application
                rootFrame.CacheSize = 1;

                // Set the default language
                rootFrame.Language = ApplicationLanguages.Languages[0];

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // Removes the turnstile navigation for startup.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (Transition c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                bool success = this.OpenApp(rootFrame);

                if (!success)
                {
                    throw new Exception("Failed to create initial page");
                }
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

        private bool OpenApp(Frame rootFrame, object o = null)
        {
            SessionData data = Storage.GetSessionData();
            if (data != null)
            {
                return rootFrame.Navigate(typeof(MainPage), o);
            }
            return rootFrame.Navigate(typeof(LoginPage), o);
        }

        /// <summary>
        ///     Restores the content transitions after the app has launched.
        /// </summary>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = (Frame) sender;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection
            {
                new NavigationThemeTransition()
            };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        /// <summary>
        ///     Invoked when application execution is being suspended. Application state is saved
        ///     without knowing whether the application will be terminated or resumed with the contents
        ///     of memory still intact.
        /// </summary>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

            // TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
