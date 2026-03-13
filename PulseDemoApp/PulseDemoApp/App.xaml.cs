// <copyright file="App.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PulseDemoApp.Utilities;
using System.IO;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PulseDemoApp;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? window;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    public Window Window => this.window;

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // In production scenarios, it is recommended to use Microsoft.Extensions.DependencyInjection
        // to register services and view models, and to resolve the initial MainWindow through the
        // service provider. This sample omits that setup to keep the code simple and focused on
        // illustrating WinUI-specific functionality.
        this.window = new MainWindow { Content = new Grid() };
        this.window.Activate();

        if (!await ConsentAsync())
            this.window.Close();
        else
        {
            this.window.Content = new MainPage(new MainViewModel());
            this.window.Closed += (_, _) => ((MainPage)this.window.Content).ViewModel.Dispose();
        }
    }

    private async Task<bool> ConsentAsync()
    {
        await Task.Delay(100);
        return await new ContentDialog
        {
            Title = "End-User-License – Agreement for Pexip Licensed Application (EULA)",
            PrimaryButtonText = "Accept",
            CloseButtonText = "Decline",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Window.Content.XamlRoot,
            Content = new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = RuntimeHelper.IsMSIX
                        ? await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/License.lic")))
                        : await File.ReadAllTextAsync("Assets/License.lic"),
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true
                }
            }
        }.ShowAsync() is ContentDialogResult.Primary;
    }
}
