// <copyright file="VideoView.xaml.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

namespace PulseDemoApp.UserControls;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using global::Windows.Graphics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

[ComImport]
[Guid("63aad0b8-7c24-40ff-85a8-640d944cc325")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public partial interface ISwapChainPanelNative
{
    [PreserveSig]
    uint SetSwapChain([In] IntPtr swapChain);
}

public sealed partial class VideoView : UserControl
{
    #region Properties

    public static readonly DependencyProperty HandleProperty =
        DependencyProperty.Register(
            nameof(Handle),
            typeof(long),
            typeof(VideoView),
            new PropertyMetadata(default(long), OnHandleChanged));

    public static readonly DependencyProperty ResizeCommandProperty =
        DependencyProperty.Register(
            nameof(ResizeCommand),
            typeof(ICommand),
            typeof(VideoView),
            new PropertyMetadata(default(ICommand)));

    #endregion

    public VideoView()
    {
        InitializeComponent();
        SizeChanged += VideoView_SizeChanged;
        Unloaded += VideoView_Unloaded;
    }

    #region Properties

    public IntPtr Handle
    {
        get => new IntPtr((long)GetValue(HandleProperty));
        set => SetValue(HandleProperty, value.ToInt64());
    }

    public ICommand ResizeCommand
    {
        get => (ICommand)GetValue(ResizeCommandProperty);
        set => SetValue(ResizeCommandProperty, value);
    }

    #endregion

    #region Event Handlers

    private void VideoView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (Handle != default)
        {
            int width = (int)Math.Ceiling(e.NewSize.Width);
            int height = (int)Math.Ceiling(e.NewSize.Height);
            ResizeCommand.Execute(new SizeInt32(width, height));
        }
    }

    private void VideoView_Unloaded(object sender, RoutedEventArgs e)
    {
        ReleaseHandle();
        SizeChanged -= VideoView_SizeChanged;
        Unloaded -= VideoView_Unloaded;
    }

    #endregion

    #region Property Changers

    private static void OnHandleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (VideoView)d;
        self.AcquireHandle();
    }

    #endregion

    private void AcquireHandle()
    {
        BindHandle(Handle);
    }

    private void ReleaseHandle()
    {
        // Setting the video handle to Zero will make sure of two things here:
        // a) Setting null to the swap chain panel -> decrease the refcount of the current swapchain in use
        // b) Calls ReleaseHandle() on the current swap chain
        BindHandle(IntPtr.Zero);
    }

    private void BindHandle(/* IDXGISwapChain1 */ IntPtr swapChainPtr)
    {
        IntPtr panelObj = IntPtr.Zero;
        IntPtr panelPtr = IntPtr.Zero;

        try
        {
            // Cast SwapChainPanel to IInspectable (IInspectable is the base interface for XAML objects in C++)
            panelObj = Marshal.GetIUnknownForObject(this.SwapChainPanel);

            // Query for ISwapChainPanelNative from the native object
            var guid = typeof(ISwapChainPanelNative).GUID;
            Marshal.QueryInterface(panelObj, ref guid, out panelPtr);

            // Cast the returned pointer to ISwapChainPanelNative
            var panelNative = (ISwapChainPanelNative)Marshal.GetObjectForIUnknown(panelPtr);

            // Call SetSwapChain with your swap chain pointer
            panelNative.SetSwapChain(swapChainPtr);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ERROR in VideoView.BindHandle: {ex.Message} {ex.StackTrace}");
        }
        finally
        {
            // Release the COM objects
            if (panelPtr != IntPtr.Zero) Marshal.Release(panelPtr);
            if (panelObj != IntPtr.Zero) Marshal.Release(panelObj);
        }
    }
}
