using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PulseDemoApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // In production scenarios, it is recommended to use Microsoft.Extensions.DependencyInjection
            // to register services and view models, and to resolve the initial MainWindow through the
            // service provider. This sample omits that setup to keep the code simple and focused on
            // illustrating WinUI-specific functionality.
            _window = new MainWindow();
            _window.Content = new MainPage(new MainViewModel());

            _window.Activate();
        }
    }
}
