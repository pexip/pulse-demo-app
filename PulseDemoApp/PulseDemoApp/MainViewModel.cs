using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Pexip.Pulse.NativeDelegates;
using Pexip.Pulse.NativeEnums;
using Pexip.Pulse.NativeMethods;
using Pexip.Pulse.NativeStructs;
using PulseDemoApp.Devices;
using PulseDemoApp.Utilities;

namespace PulseDemoApp;

public partial class MainViewModel : ObservableObject
{
    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private readonly IntPtr pulseInstance = PulseConnect.pulse_new();

    #region Properties

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
    private bool joinCardEnabled;

    [ObservableProperty]
    private bool hostPinCardEnabled;

    [ObservableProperty]
    private bool conferenceCardEnabled;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string videoAddress;

    [ObservableProperty]
    private string registrationProgress;

    [ObservableProperty]
    private MediaDevice[] cameras;

    [ObservableProperty]
    private MediaDevice[] mics;

    [ObservableProperty]
    private MediaDevice[] speakers;
    #endregion

    public MainViewModel()
    {
        this.videoAddress = string.Empty;        
        intitJoinPage();
    }

    private void intitJoinPage()
    {        
        VideoAddress = "sunjay.kalsi@nightly.pexip.com";
    }

    private void ReadVideoDevices()
    {
        var devices = new List<MediaDevice>();

        PulseErrorType pulseError = PulseDevices.pulse_device_iterator_new(pulseInstance,
            PulseMediaType.PULSE_MEDIA_VIDEO, PulseMediaDirection.PULSE_MEDIA_OUTPUT, out IntPtr p_iterator);

        PulseDeviceIteratorFunc appendDevice = (device, user_context) =>
        {
            devices.Add(new MediaDevice(device.id, device.name, true, true));
        };

        pulseError = PulseDevices.pulse_device_iterator_foreach(p_iterator, appendDevice, IntPtr.Zero); // in the PulseClient the last param is "pinnedDevices.Ptr" - I don't know what that is

        Debug.Print(devices.Count > 0 ? $"Found {devices.Count} video devices" : "No video devices found");

        PulseDevices.pulse_device_iterator_free(p_iterator);
    }

    #region Commands

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        JoinCardEnabled = await RegisterWithSsoAsync();

        if (JoinCardEnabled)
        {
            ReadVideoDevices();
        }
    }

    [RelayCommand(CanExecute = nameof(CanJoin))]
    private async Task Join()
    {
    }
    #endregion

    #region Command Validations

    private bool CanRegister()
    {
        return new AliasValidator().ValidateFullyQualifiedAlias(VideoAddress);
    }
    private bool CanJoin() => JoinCardEnabled;
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
}
