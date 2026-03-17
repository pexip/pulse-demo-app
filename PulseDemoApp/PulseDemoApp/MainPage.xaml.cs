// SPDX-FileCopyrightText: 2026 Copyright 2026 Pexip AS
//
// SPDX-License-Identifier: Apache-2.0

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
