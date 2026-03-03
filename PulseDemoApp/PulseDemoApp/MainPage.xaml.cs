// <copyright file="MainPage.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PulseDemoApp;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainPage : Page
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
    }

    public MainViewModel ViewModel { get; }
}
