// SPDX-FileCopyrightText: 2026 Copyright 2026 Pexip AS
//
// SPDX-License-Identifier: Apache-2.0

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Pexip.Pulse.NativeEnums;
using Pexip.Pulse.NativeMethods;
using Pexip.Pulse.NativeStructs;
using PulseDemoApp.UserControls;
using PulseDemoApp.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using Windows.Graphics;
using Windows.Storage;

namespace PulseDemoApp;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private readonly IntPtr pulseInstance = PulseConnect.pulse_new();
    
    private PulseRegistrationStatusCallbackConfig registrationStateCallback;
    private PulseSSOProviderCallbackConfig ssoProviderCallback;
    private PulseAsyncOperationResultCallbackConfig registerResultCallback;
    private PulseOperationProgressCallbackConfig registerProgressCallback;
    private PulseConferenceStatusCallbackConfig conferenceStateCallback;
    private PulseAsyncOperationResultCallbackConfig connectResultCallback;
    private PulseOperationProgressCallbackConfig connectProgressCallback;
    private PulseAsyncOperationResultCallbackConfig disconnectResultCallback;
    private PulseOperationProgressCallbackConfig disconnectProgressCallback;

    private enum CardType
    {
        Registration,
        Connection,
        Join,
        Conference
    }
    private bool disposed;

    #region Window Properties

    [ObservableProperty]
    private string registrationCardLogs;

    [ObservableProperty]
    private bool connectionCardEnabled;

    [ObservableProperty]
    private bool joinCardEnabled;

    [ObservableProperty]
    private string joinCardLogs;

    [ObservableProperty]
    private bool conferenceCardEnabled;

    [ObservableProperty]
    private string conferenceCardLogs;

    #endregion

    #region Registration Card Properties

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string videoAddress;

    #endregion

    #region Connection Card Properties

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private string videoAlias;

    [ObservableProperty]
    private string pinCode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private string displayName;

    #endregion

    #region Join Card Properties

    [ObservableProperty]
    private IntPtr? selfVideoHandle;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
    private PulseDevice? selectedCamera;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
    private PulseDevice? selectedMicrophone;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
    private PulseDevice? selectedSpeaker;

    [ObservableProperty]
    private IList<PulseDevice> cameras;

    [ObservableProperty]
    private IList<PulseDevice> microphones;

    [ObservableProperty]
    private IList<PulseDevice> speakers;

    #endregion

    #region Conference Card Properties

    [ObservableProperty]
    private IntPtr? mainVideoHandle;

    #endregion

    public MainViewModel()
    {
        this.cameras = ReadDevices(PulseMediaType.PULSE_MEDIA_VIDEO, PulseMediaDirection.PULSE_MEDIA_INPUT);
        this.microphones = ReadDevices(PulseMediaType.PULSE_MEDIA_AUDIO, PulseMediaDirection.PULSE_MEDIA_INPUT);
        this.speakers = ReadDevices(PulseMediaType.PULSE_MEDIA_AUDIO, PulseMediaDirection.PULSE_MEDIA_OUTPUT);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        if (disposing)
        {
            // Disconnect if needed
            bool connected = PulseConnect.pulse_is_connected(this.pulseInstance);
            if (connected) PulseConnect.pulse_disconnect(this.pulseInstance, default);

            // Destroy Video handles
            if (SelfVideoHandle.HasValue) PulseDeviceSession.pulse_device_session_release_video_handle(this.pulseInstance, SelfVideoHandle.Value);
            if (MainVideoHandle.HasValue) PulseDeviceSession.pulse_device_session_release_video_handle(this.pulseInstance, MainVideoHandle.Value);

            // Destroy Pulse instance
            PulseConnect.pulse_free(this.pulseInstance);
        }

        this.disposed = true;
    }

    #region Commands

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        ConnectionCardEnabled = await RegisterWithSsoAsync();
    }

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue()
    {
        JoinCardEnabled = true;
    }

    [RelayCommand]
    private void PickCamera(VideoView view)
    {
        SelfVideoHandle ??= PulseDeviceSession.pulse_device_session_create_video_handle(this.pulseInstance, PulseMediaContent.PULSE_MEDIA_CONTENT_SELFVIEW, (int)view.ActualWidth, (int)view.ActualHeight, (ulong)"#212121".ToColor().ToInt());
        ReportError(
            PulseDeviceSession.pulse_device_session_connect_device(this.pulseInstance, SelectedCamera!.Value, PulseMediaContent.PULSE_MEDIA_CONTENT_MAIN),
            CardType.Join);
    }

    [RelayCommand]
    private void PickMicrophone()
    {
        ReportError(
            PulseDeviceSession.pulse_device_session_connect_device(this.pulseInstance, SelectedMicrophone!.Value, PulseMediaContent.PULSE_MEDIA_CONTENT_MAIN),
            CardType.Join);
    }

    [RelayCommand]
    private void PickSpeaker()
    {
        ReportError(
            PulseDeviceSession.pulse_device_session_connect_device(this.pulseInstance, SelectedSpeaker!.Value, PulseMediaContent.PULSE_MEDIA_CONTENT_MAIN),
            CardType.Join);
    }

    [RelayCommand]
    private void ResizeSelfView(SizeInt32 size)
    {
        if (SelfVideoHandle.HasValue)
        {

            ReportError(
            PulseDeviceSession.pulse_device_session_resize_video_handle(this.pulseInstance, SelfVideoHandle.Value, size.Width, size.Height),
            CardType.Join);
        }
    }

    [RelayCommand]
    private void ResizeMainView(SizeInt32 size)
    {
        if (MainVideoHandle.HasValue)
        {
            ReportError(
                PulseDeviceSession.pulse_device_session_resize_video_handle(this.pulseInstance, MainVideoHandle.Value, size.Width, size.Height),
                CardType.Conference);
        }
    }

    [RelayCommand(CanExecute = nameof(CanJoin))]
    private async Task JoinAsync(VideoView videoView)
    {
        MainVideoHandle ??= PulseDeviceSession.pulse_device_session_create_video_handle(this.pulseInstance, PulseMediaContent.PULSE_MEDIA_CONTENT_MAIN, (int)videoView.ActualWidth, (int)videoView.ActualHeight, (ulong)"#FF212121".ToColor().ToInt());
        ConferenceCardEnabled = await JoinConferenceAsync(
            VideoAlias.Contains('@')
                ? VideoAlias[(VideoAlias.IndexOf('@') + 1)..]
                : VideoAddress[(VideoAddress.IndexOf('@') + 1)..]);
    }

    [RelayCommand]
    private async Task LeaveAsync()
    {
        ConferenceCardEnabled = !await LeaveConferenceAsync();
    }

    [RelayCommand]
    private async Task DisplayLicensesAsync(XamlRoot xamlRoot)
    {
        await new ContentDialog
        {
            Title = "Open-Source License Information",
            PrimaryButtonText = "OK",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot,
            Content = new ScrollViewer
            {
                Content = new ItemsRepeater
                {
                    ItemsSource = new ObservableCollection<string>(
                        RuntimeHelper.IsMSIX
                            ? await FileIO.ReadLinesAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Licenses.txt")))
                            : await File.ReadAllLinesAsync("Assets/Licenses.txt")),
                    ItemTemplate = (DataTemplate)XamlReader.Load(
                        @"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                            <TextBlock
                                Text=""{Binding}""
                                TextWrapping=""Wrap""
                                IsTextSelectionEnabled=""True"" />
                        </DataTemplate>"),
                },
            }
        }.ShowAsync();
    }

    #endregion

    #region Command Validations

    private bool CanRegister() =>
        VideoAddress != null && AliasValidator.ValidateFullyQualifiedAlias(VideoAddress);

    private bool CanContinue() =>
        !string.IsNullOrWhiteSpace(DisplayName) &&
        VideoAlias != null && AliasValidator.ValidateRegisteredAlias(VideoAlias);

    private bool CanJoin() =>
        SelectedCamera != null &&
        SelectedMicrophone != null &&
        SelectedSpeaker != null;

    #endregion

    private List<PulseDevice> ReadDevices(PulseMediaType mediaType, PulseMediaDirection mediaDirection)
    {
        ReportError(
            PulseDevices.pulse_device_iterator_new(this.pulseInstance, mediaType, mediaDirection, out IntPtr p_iterator),
            CardType.Join);

        var devices = new List<PulseDevice>();
        ReportError(
            PulseDevices.pulse_device_iterator_foreach(
                p_iterator,
                (device, user_context) => devices.Add(device),
                0),
            CardType.Join);

        PulseDevices.pulse_device_iterator_free(p_iterator);

        return devices;
    }

    private async Task<bool> RegisterWithSsoAsync()
    {
        var promise = new TaskCompletionSource<bool>();

        this.registrationStateCallback = new PulseRegistrationStatusCallbackConfig
        {
            func = (status_info, user_context) =>
            {
                if (status_info.status == PulseConnectionStatus.PULSE_CONNECTION_STATUS_CONNECTED)
                {
                    promise.TrySetResult(true);
                }
            }
        };
        ReportError(
            PulseOptions.pulse_options_set_registration_state_callback(
                this.pulseInstance,
                this.registrationStateCallback),
            CardType.Registration);
        
        this.ssoProviderCallback = new PulseSSOProviderCallbackConfig
        {
            selection_callback = (ref PulseSSOProviderList list, IntPtr user_context) =>
            {
                var providers = list.providers.ToStructs<PulseSSOProvider>(list.num, false);
                bool accepted = true; // Waiting for the user to accept the ssoProvider(s)
                return accepted ? 0 : -1;
            },
            request_callback = (PulseSSOProviderRequest request, PulseSSOProviderSetToken setToken, IntPtr user_context) =>
            {
                string? ssoToken = RequestSSOToken(new Uri(request.url)); // Trigger a SSO token request
                return ssoToken is string token && setToken.func(setToken.context, token);
            },
        };
        ReportError(
            PulseOptions.pulse_options_set_sso_provider_callbacks(
                this.pulseInstance,
                this.ssoProviderCallback),
            CardType.Registration);

        this.registerResultCallback = new PulseAsyncOperationResultCallbackConfig
        {
            func = (err, user_context) =>
            {
                if (err != PulseErrorType.PULSE_SUCCESS)
                {
                    ReportError(err, CardType.Registration);
                    promise.TrySetResult(false);
                }
            }
        };
        this.registerProgressCallback = new PulseOperationProgressCallbackConfig
        {
            func = (progress_info, user_context) =>
            {
                var info = progress_info.ToStruct<PulseOperationProgressInfo>();
                this.dispatcherQueue.TryEnqueue(() => RegistrationCardLogs = $"{info.progress:P0} {info.desc}"); // Report Progress
            }
        };
        PulseErrorType error = PulseRegistration.pulse_register_async(
            this.pulseInstance,
            new PulseRegistrationRequest
            {
                alias = VideoAddress,
                host = VideoAddress[(VideoAddress.IndexOf('@') + 1)..],
                use_sso = true,
            },
            this.registerResultCallback,
            this.registerProgressCallback);

        var success = error is PulseErrorType.PULSE_SUCCESS && await promise.Task.ConfigureAwait(false);

        return success;
    }

    private string? RequestSSOToken(Uri url)
    {
        // Spin up the default browser
        using var p = new Process();
        p.StartInfo.FileName = url.ToString();
        p.StartInfo.UseShellExecute = true; // use the default browser
        p.StartInfo.RedirectStandardOutput = false; // we do not want the standard output here
        p.StartInfo.CreateNoWindow = false; // the browser should show up in a separate window
        p.Start();

        IntPtr handle = PulseIPC.pulse_ipc_new("PulseWinClientSSOPipe", 16 * 1024);

        // Waiting for the SSO completion
        PulseErrorType error = PulseIPC.pulse_ipc_read_line(handle, out IntPtr pData, IntPtr.Zero, 1000, 60 * 1000);
        string? token = null;
        if (error == PulseErrorType.PULSE_SUCCESS)
        {
            string[] parts = pData.ToString(Encoding.ASCII).Split('=');
            if (parts.Length > 1)
            {
                token = parts[1];
            }
        }

        PulseIPC.pulse_ipc_free(handle);

        return token;
    }

    private async Task<bool> JoinConferenceAsync(string server)
    {
        var promise = new TaskCompletionSource<bool>();

        this.conferenceStateCallback = new PulseConferenceStatusCallbackConfig
        {
            func = (status_info, user_context) =>
            {
                if (status_info.status == PulseConnectionStatus.PULSE_CONNECTION_STATUS_CONNECTED)
                {
                    promise.TrySetResult(true);
                }
            },
        };
        PulseOptions.pulse_options_set_conference_state_callback(
            this.pulseInstance,
            this.conferenceStateCallback);

        this.connectResultCallback = new PulseAsyncOperationResultCallbackConfig
        {
            func = (err, user_context) =>
            {
                if (err != PulseErrorType.PULSE_SUCCESS)
                {
                    ReportError(err, CardType.Join);
                    promise.TrySetResult(false);
                }
            },
        };
        this.connectProgressCallback = new PulseOperationProgressCallbackConfig
        {
            func = (progress_info, user_context) =>
            {
                var info = progress_info.ToStruct<PulseOperationProgressInfo>();
                this.dispatcherQueue.TryEnqueue(() => JoinCardLogs = $"{info.progress:P0} {info.desc}"); // Report Progress
            },
        };
        var error = PulseConnect.pulse_connect_with_rest_async(
            this.pulseInstance,
            new PulseRestConnectionConfig
            {
                server_address = server,
                display_name = DisplayName,
                conference_name = VideoAlias,
                pin_code = PinCode,
            },
            this.connectResultCallback,
            this.connectProgressCallback);

        return error is PulseErrorType.PULSE_SUCCESS && await promise.Task.ConfigureAwait(false);
    }

    private async Task<bool> LeaveConferenceAsync()
    {
        var promise = new TaskCompletionSource<bool>();

        this.disconnectResultCallback = new PulseAsyncOperationResultCallbackConfig
        {
            func = (err, user_context) =>
            {
                if (err != PulseErrorType.PULSE_SUCCESS)
                {
                    ReportError(err, CardType.Conference);
                    promise.TrySetResult(false);
                }
                else
                {
                    this.dispatcherQueue.TryEnqueue(() => ConferenceCardLogs = string.Empty);
                    this.dispatcherQueue.TryEnqueue(() => JoinCardLogs = string.Empty);
                    promise.TrySetResult(true);
                }
            }
        };
        this.disconnectProgressCallback = new PulseOperationProgressCallbackConfig
        {
            func = (progress_info, user_context) =>
            {
                var info = progress_info.ToStruct<PulseOperationProgressInfo>();
                this.dispatcherQueue.TryEnqueue(() => ConferenceCardLogs = $"{info.progress:P0} {info.desc}"); // Report Progress
            }
        };
        var error = PulseConnect.pulse_disconnect_async(
            this.pulseInstance,
            this.disconnectResultCallback,
            this.disconnectProgressCallback);

        return error is PulseErrorType.PULSE_SUCCESS && await promise.Task.ConfigureAwait(false);
    }

    private void ReportError(PulseErrorType err, CardType cardType)
    {
        if (err != PulseErrorType.PULSE_SUCCESS)
        {
            string errMessage = PulseError.pulse_strerror(err).ToString(Encoding.ASCII);
            Action logOnCard = cardType switch
            {
                CardType.Registration => () => RegistrationCardLogs = errMessage,
                CardType.Join => () => JoinCardLogs = errMessage,
                CardType.Conference => () => ConferenceCardLogs = errMessage,
                _ => throw new NotImplementedException(),
            };

            if (this.dispatcherQueue.HasThreadAccess) logOnCard();
            else this.dispatcherQueue.TryEnqueue(() => logOnCard());
        }
    }
}
