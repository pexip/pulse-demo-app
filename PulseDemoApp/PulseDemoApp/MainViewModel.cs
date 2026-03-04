// <copyright file="MainViewModel.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Pexip.Pulse.NativeDelegates;
using Pexip.Pulse.NativeEnums;
using Pexip.Pulse.NativeMethods;
using Pexip.Pulse.NativeStructs;
using PulseDemoApp.Devices;
using PulseDemoApp.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.Graphics;

namespace PulseDemoApp;

public partial class MainViewModel : ObservableObject
{
    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private readonly IntPtr pulseInstance = PulseConnect.pulse_new();

    #region Properties

    [ObservableProperty]
    private MediaDevice? selectedJoinCameraDevice;

    [ObservableProperty]
    private MediaDevice? selectedJoinMicDevice;

    [ObservableProperty]
    private MediaDevice? selectedJoinSpeakerDevice;

    [ObservableProperty]
    private IntPtr selfPreviewHandle;

    [ObservableProperty]
    private IntPtr mainConferenceHandle;

    [ObservableProperty]
    private bool conferenceViewActive;

    [ObservableProperty]
    private bool metingAliasCardEnabled;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
    private bool joinCardEnabled;

    [ObservableProperty]
    private bool conferenceCardEnabled;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string videoAddress = string.Empty;

    [ObservableProperty]
    private string registrationProgress;

    [ObservableProperty]
    private string connectionProgress;

    [ObservableProperty]
    private string disconnectionProgress;

    [ObservableProperty]
    private MediaDevice[] cameras;

    [ObservableProperty]
    private MediaDevice[] mics;

    [ObservableProperty]
    private MediaDevice[] speakers;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private string videoAlias;

    private nint userContext;
    private Microsoft.UI.Xaml.Controls.SwapChainPanel? cameraPreviewPanel;

    #endregion

    public MainViewModel()
    {
    }

    #region Commands

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        MetingAliasCardEnabled = await RegisterWithSsoAsync();
    }

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue()
    {
        JoinCardEnabled = true;

        SelfPreviewHandle = PulseDeviceSession.pulse_device_session_create_video_handle(pulseInstance, PulseMediaContent.PULSE_MEDIA_CONTENT_SELFVIEW, 466, 306, 0xFF000000);
        ReadDevices(PulseMediaType.PULSE_MEDIA_VIDEO, PulseMediaDirection.PULSE_MEDIA_INPUT);
        ReadDevices(PulseMediaType.PULSE_MEDIA_AUDIO, PulseMediaDirection.PULSE_MEDIA_INPUT);
        ReadDevices(PulseMediaType.PULSE_MEDIA_AUDIO, PulseMediaDirection.PULSE_MEDIA_OUTPUT);
    }

    [RelayCommand]
    private void ResizeSelfPrimary(SizeInt32 size)
    {
        var error = PulseDeviceSession.pulse_device_session_resize_video_handle(this.pulseInstance, SelfPreviewHandle, size.Width, size.Height);
    }

    [RelayCommand]
    private void ResizeConference(SizeInt32 size)
    {
        var error = PulseDeviceSession.pulse_device_session_resize_video_handle(this.pulseInstance, MainConferenceHandle, size.Width, size.Height);
    }

    [RelayCommand(CanExecute = nameof(CanJoin))]
    private async Task JoinAsync()
    {
        var server = VideoAlias.Contains('@') ? VideoAlias[(VideoAlias.IndexOf('@') + 1)..] : VideoAddress[(VideoAddress.IndexOf('@') + 1)..];
        ConferenceCardEnabled = await JoinConferenceAsync(server, "your name", VideoAlias, "543419");
    }

    [RelayCommand]
    private async Task LeaveAsync()
    {
        ConferenceCardEnabled = !await LeaveConferenceAsync();
    }

    #endregion

    #region Command Validations

    private bool CanRegister()
    {
        return new AliasValidator().ValidateFullyQualifiedAlias(VideoAddress);
    }

    private bool CanContinue()
    {
        return VideoAlias != null && new AliasValidator().ValidateRegisteredAlias(VideoAlias);
    }

    private bool CanJoin() => JoinCardEnabled;

    #endregion

    partial void OnSelectedJoinCameraDeviceChanged(MediaDevice? value)
    {
        ConnectMediaDevice(value, "Camera");
    }

    partial void OnSelectedJoinSpeakerDeviceChanged(MediaDevice? value)
    {
        ConnectMediaDevice(value, "Speaker");
    }

    partial void OnSelectedJoinMicDeviceChanged(MediaDevice? value)
    {
        ConnectMediaDevice(value, "Microphone");
    }

    #region Device Connection Helpers

    private void ReadDevices(PulseMediaType mediaType, PulseMediaDirection mediaDirection)
    {
        var devices = new List<MediaDevice>();

        devices.Add(new MediaDevice(0, "None", mediaType, mediaDirection, 0, false, false));

        PulseErrorType pulseError = PulseDevices.pulse_device_iterator_new(pulseInstance, mediaType, mediaDirection, out IntPtr p_iterator);

        using (var iterator = new PulseDeviceIteratorHandle(p_iterator))
        {
            PulseDeviceIteratorFunc addDevice = (device, user_context) =>
            {
                devices.Add(new MediaDevice(device.id, device.name, device.media_type, device.media_direction, device.on_list, device.is_default != 0, true));
            };

            pulseError = PulseDevices.pulse_device_iterator_foreach(iterator.Handle, addDevice, 0);

            switch ((mediaType, mediaDirection))
            {
                case (PulseMediaType.PULSE_MEDIA_VIDEO, PulseMediaDirection.PULSE_MEDIA_INPUT):
                    Cameras = (Cameras ?? Array.Empty<MediaDevice>()).Concat(devices).ToArray();
                    break;
                case (PulseMediaType.PULSE_MEDIA_AUDIO, PulseMediaDirection.PULSE_MEDIA_INPUT):
                    Mics = (Mics ?? Array.Empty<MediaDevice>()).Concat(devices).ToArray();
                    break;
                case (PulseMediaType.PULSE_MEDIA_AUDIO, PulseMediaDirection.PULSE_MEDIA_OUTPUT):
                    Speakers = (Speakers ?? Array.Empty<MediaDevice>()).Concat(devices).ToArray();
                    break;
            }
        }
    }

    private bool ConnectMediaDevice(MediaDevice device, string deviceTypeName)
    {
        PulseErrorType error = PulseDeviceSession.pulse_device_session_connect_device(pulseInstance, device.ToPulseDevice(), PulseMediaContent.PULSE_MEDIA_CONTENT_MAIN);

        if (error != PulseErrorType.PULSE_SUCCESS)
        {
            device.IsConnected = false;
            return false;
        }

        device.IsConnected = true;
        return true;
    }

    #endregion

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
                    string? ssoToken = RequestSSOToken(new Uri(request.url), request.is_reconnect); // Trigger a SSO token request
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

    private string? RequestSSOToken(Uri url, bool is_reconnect)
    {
        if (url.Scheme != "https")
        {
            return null;
        }

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

    private async Task<bool> JoinConferenceAsync(string server, string displayName, string conferenceName, string pinCode)
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
                display_name = displayName,
                conference_name = conferenceName,
                pin_code = pinCode,
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
