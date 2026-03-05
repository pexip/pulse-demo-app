// <copyright file="MainViewModel.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Dispatching;
using Pexip.Pulse.NativeEnums;
using Pexip.Pulse.NativeMethods;
using Pexip.Pulse.NativeStructs;
using PulseDemoApp.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using Windows.Graphics;
using Windows.Media.Devices;

namespace PulseDemoApp;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private readonly IntPtr pulseInstance = PulseConnect.pulse_new();

    #region Window Properties

    [ObservableProperty]
    private bool connectionCardEnabled;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
    private bool joinCardEnabled;

    [ObservableProperty]
    private bool conferenceCardEnabled;

    #endregion

    #region Registration Card Properties

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string videoAddress;

    [ObservableProperty]
    private string registrationProgress;

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

    [ObservableProperty]
    private string connectionProgress;

    [ObservableProperty]
    private string disconnectionProgress;

    #endregion

    public void Dispose()
    {
        // Disconnect if needed
        bool connected = PulseConnect.pulse_is_connected(this.pulseInstance);
        if (connected) PulseConnect.pulse_disconnect(this.pulseInstance, default);

        // Destroy Pulse instance
        PulseConnect.pulse_free(this.pulseInstance);
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

        Cameras = ReadDevices(PulseMediaType.PULSE_MEDIA_VIDEO, PulseMediaDirection.PULSE_MEDIA_INPUT);
        Microphones = ReadDevices(PulseMediaType.PULSE_MEDIA_AUDIO, PulseMediaDirection.PULSE_MEDIA_INPUT);
        Speakers = ReadDevices(PulseMediaType.PULSE_MEDIA_AUDIO, PulseMediaDirection.PULSE_MEDIA_OUTPUT);
    }

    [RelayCommand]
    private void ResizeSelfView(SizeInt32 size)
    {
        PulseDeviceSession.pulse_device_session_resize_video_handle(this.pulseInstance, SelfVideoHandle.Value, size.Width, size.Height);
    }

    [RelayCommand]
    private void ResizeMainView(SizeInt32 size)
    {
        PulseDeviceSession.pulse_device_session_resize_video_handle(this.pulseInstance, MainVideoHandle.Value, size.Width, size.Height);
    }

    [RelayCommand(CanExecute = nameof(CanJoin))]
    private async Task JoinAsync()
    {
        MainVideoHandle ??= PulseDeviceSession.pulse_device_session_create_video_handle(this.pulseInstance, PulseMediaContent.PULSE_MEDIA_CONTENT_MAIN, 366, 206, (ulong)"#FF212121".ToColor().ToInt());
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

    #endregion

    #region Command Validations

    private bool CanRegister() =>
        VideoAddress != null && new AliasValidator().ValidateFullyQualifiedAlias(VideoAddress);

    private bool CanContinue() =>
        !string.IsNullOrWhiteSpace(DisplayName) &&
        VideoAlias != null && new AliasValidator().ValidateRegisteredAlias(VideoAlias);

    private bool CanJoin() =>
        SelectedCamera != null &&
        SelectedMicrophone != null &&
        SelectedSpeaker != null;

    #endregion

    #region Property Changers

    partial void OnSelectedCameraChanged(PulseDevice? value)
    {
        SelfVideoHandle ??= PulseDeviceSession.pulse_device_session_create_video_handle(this.pulseInstance, PulseMediaContent.PULSE_MEDIA_CONTENT_SELFVIEW, 196, 110, (ulong)"#212121".ToColor().ToInt());
        PulseErrorType error = PulseDeviceSession.pulse_device_session_connect_device(this.pulseInstance, value.Value, PulseMediaContent.PULSE_MEDIA_CONTENT_MAIN);
    }

    partial void OnSelectedSpeakerChanged(PulseDevice? value)
    {
        PulseErrorType error = PulseDeviceSession.pulse_device_session_connect_device(this.pulseInstance, value.Value, PulseMediaContent.PULSE_MEDIA_CONTENT_MAIN);
    }

    partial void OnSelectedMicrophoneChanged(PulseDevice? value)
    {
        PulseErrorType error = PulseDeviceSession.pulse_device_session_connect_device(this.pulseInstance, value.Value, PulseMediaContent.PULSE_MEDIA_CONTENT_MAIN);
    }

    #endregion

    private List<PulseDevice> ReadDevices(PulseMediaType mediaType, PulseMediaDirection mediaDirection)
    {
        PulseErrorType pulseError = PulseDevices.pulse_device_iterator_new(this.pulseInstance, mediaType, mediaDirection, out IntPtr p_iterator);

        var devices = new List<PulseDevice>();
        pulseError = PulseDevices.pulse_device_iterator_foreach(
            p_iterator,
            (device, user_context) => devices.Add(device),
            0);

        PulseDevices.pulse_device_iterator_free(p_iterator);

        return devices;
    }

    private async Task<bool> RegisterWithSsoAsync()
    {
        var promise = new TaskCompletionSource<bool>();

        // Set registration events callbacks
        PulseOptions.pulse_options_set_registration_state_callback(
            this.pulseInstance,
            new PulseRegistrationStatusCallbackConfig
            {
                func = (status_info, user_context) =>
                {
                    if (status_info.status == PulseConnectionStatus.PULSE_CONNECTION_STATUS_CONNECTED)
                    {
                        promise.TrySetResult(true);
                    }
                }
            });

        // Set SSO callbacks
        PulseOptions.pulse_options_set_sso_provider_callbacks(
            this.pulseInstance,
            new PulseSSOProviderCallbackConfig
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
            });

        // Register
        PulseErrorType error = PulseRegistration.pulse_register_async(
            this.pulseInstance,
            new PulseRegistrationRequest
            {
                alias = VideoAddress,
                host = VideoAddress[(VideoAddress.IndexOf('@') + 1)..],
                use_sso = true,
            },
            new PulseAsyncOperationResultCallbackConfig
            {
                func = (err, user_context) =>
                {
                    if (err != PulseErrorType.PULSE_SUCCESS)
                    {
                        this.dispatcherQueue.TryEnqueue(() => RegistrationProgress = $"{PulseError.pulse_strerror(err).ToString(Encoding.ASCII)}");
                        promise.TrySetResult(false);
                    }
                }
            },
            new PulseOperationProgressCallbackConfig
            {
                func = (progress_info, user_context) =>
                {
                    var info = progress_info.ToStruct<PulseOperationProgressInfo>();
                    this.dispatcherQueue.TryEnqueue(() => RegistrationProgress = $"{info.progress:P0} {info.desc}"); // Report Progress
                }
            });

        var success = error is PulseErrorType.PULSE_SUCCESS && await promise.Task.ConfigureAwait(false);

        return success;
    }

    private string? RequestSSOToken(Uri url)
    {
        // Spin up the default browser
        Process p = new Process();
        p.StartInfo.FileName = url.ToString();
        p.StartInfo.UseShellExecute = true; // use the default browser
        p.StartInfo.RedirectStandardOutput = false; // we do not want the standard output here
        p.StartInfo.CreateNoWindow = false; // the browser should show up in a separate window
        p.Start();

        IntPtr handle = PulseIPC.pulse_ipc_new("PulseWinClientSSOPipe", 16 * 1024);

        // Waiting for the SSO completion
        PulseErrorType error = PulseIPC.pulse_ipc_read_line(handle, out IntPtr pData, IntPtr.Zero, 1000, 60 * 1000);
        string? token = error == PulseErrorType.PULSE_SUCCESS
            ? pData.ToString(Encoding.ASCII).Split('=')[1]
            : null;

        PulseIPC.pulse_ipc_free(handle);

        return token;
    }

    private async Task<bool> JoinConferenceAsync(string server)
    {
        var promise = new TaskCompletionSource<bool>();

        // Set conference events callbacks
        PulseOptions.pulse_options_set_conference_state_callback(
            this.pulseInstance,
            new PulseConferenceStatusCallbackConfig
            {
                func = (status_info, user_context) =>
                {
                    if (status_info.status == PulseConnectionStatus.PULSE_CONNECTION_STATUS_CONNECTED)
                    {
                        promise.TrySetResult(true);
                    }
                },
            });

        // Connect
        var error = PulseConnect.pulse_connect_with_rest_async(
            this.pulseInstance,
            new PulseRestConnectionConfig
            {
                server_address = server,
                display_name = DisplayName,
                conference_name = VideoAlias,
                pin_code = PinCode,
            },
            new PulseAsyncOperationResultCallbackConfig
            {
                func = (err, user_context) =>
                {
                    if (err != PulseErrorType.PULSE_SUCCESS)
                    {
                        this.dispatcherQueue.TryEnqueue(() => ConnectionProgress = $"{PulseError.pulse_strerror(err).ToString(Encoding.ASCII)}");
                        promise.TrySetResult(false);
                    }
                },
            },
            new PulseOperationProgressCallbackConfig
            {
                func = (progress_info, user_context) =>
                {
                    var info = progress_info.ToStruct<PulseOperationProgressInfo>();
                    this.dispatcherQueue.TryEnqueue(() => ConnectionProgress = $"{info.progress:P0} {info.desc}"); // Report Progress
                },
            });

        return error is PulseErrorType.PULSE_SUCCESS && await promise.Task.ConfigureAwait(false);
    }

    private async Task<bool> LeaveConferenceAsync()
    {
        var promise = new TaskCompletionSource<bool>();

        // Disconnect
        var error = PulseConnect.pulse_disconnect_async(
            this.pulseInstance,
            new PulseAsyncOperationResultCallbackConfig
            {
                func = (err, user_context) =>
                {
                    if (err != PulseErrorType.PULSE_SUCCESS)
                    {
                        this.dispatcherQueue.TryEnqueue(() => DisconnectionProgress = $"{PulseError.pulse_strerror(err).ToString(Encoding.ASCII)}");
                        promise.TrySetResult(false);
                    }
                    else
                    {
                        this.dispatcherQueue.TryEnqueue(() => DisconnectionProgress = string.Empty);
                        this.dispatcherQueue.TryEnqueue(() => ConnectionProgress = string.Empty);
                        promise.TrySetResult(true);
                    }
                }
            },
            new PulseOperationProgressCallbackConfig
            {
                func = (progress_info, user_context) =>
                {
                    var info = progress_info.ToStruct<PulseOperationProgressInfo>();
                    this.dispatcherQueue.TryEnqueue(() => DisconnectionProgress = $"{info.progress:P0} {info.desc}"); // Report Progress
                }
            });

        return error is PulseErrorType.PULSE_SUCCESS && await promise.Task.ConfigureAwait(false);
    }
}
