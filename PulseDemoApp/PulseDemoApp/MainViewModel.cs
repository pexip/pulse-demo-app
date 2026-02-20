using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Pexip.Pulse.NativeDelegates;
using Pexip.Pulse.NativeEnums;
using Pexip.Pulse.NativeMethods;
using Pexip.Pulse.NativeStructs;
using PulseDemoApp.Devices;
using PulseDemoApp.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PulseDemoApp;

public partial class MainViewModel : ObservableObject
{
    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private readonly IntPtr pulseInstance = PulseConnect.pulse_new();

    #region Properties

    [ObservableProperty]
    private bool metingAliasCardEnabled;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
    private bool joinCardEnabled;

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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private string videoAlias;

    private nint VideoHandle;

    private nint userContext;
    private Microsoft.UI.Xaml.Controls.SwapChainPanel? cameraPreviewPanel;
    #endregion

    public MainViewModel()
    {
        this.videoAddress = string.Empty;
        intitJoinPage();
    }

    private void intitJoinPage()
    {
    }

    private void ReadDevices(PulseMediaType mediaType, PulseMediaDirection mediaDirection)
    {
        var devices = new List<MediaDevice>();

        PulseErrorType pulseError = PulseDevices.pulse_device_iterator_new(pulseInstance, mediaType, mediaDirection, out IntPtr p_iterator);

        PulseDeviceIteratorFunc addDevice = (device, user_context) =>
        {
            devices.Add(new MediaDevice(device.id, device.name, true, true));
        };

        pulseError = PulseDevices.pulse_device_iterator_foreach(p_iterator, addDevice, 0);

        if (mediaType == PulseMediaType.PULSE_MEDIA_VIDEO && mediaDirection == PulseMediaDirection.PULSE_MEDIA_INPUT)
        {
            Cameras = (Cameras ?? Array.Empty<MediaDevice>()).Concat(devices).ToArray();
        }
        else if (mediaType == PulseMediaType.PULSE_MEDIA_AUDIO && mediaDirection == PulseMediaDirection.PULSE_MEDIA_INPUT)
        {
            Mics = (Mics ?? Array.Empty<MediaDevice>()).Concat(devices).ToArray();
        }
        else if (mediaType == PulseMediaType.PULSE_MEDIA_AUDIO && mediaDirection == PulseMediaDirection.PULSE_MEDIA_OUTPUT)
        {
            Speakers = (Speakers ?? Array.Empty<MediaDevice>()).Concat(devices).ToArray();
        }

        PulseDevices.pulse_device_iterator_free(p_iterator);
    }

    #region Commands

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        MetingAliasCardEnabled = await RegisterWithSsoAsync();
    }

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue(Microsoft.UI.Xaml.Controls.SwapChainPanel? panel)
    {
        JoinCardEnabled = true;

        if (JoinCardEnabled && panel != null)
        {
            cameraPreviewPanel = panel;
            ReadDevices(PulseMediaType.PULSE_MEDIA_VIDEO, PulseMediaDirection.PULSE_MEDIA_INPUT);
            ReadDevices(PulseMediaType.PULSE_MEDIA_AUDIO, PulseMediaDirection.PULSE_MEDIA_INPUT);
            ReadDevices(PulseMediaType.PULSE_MEDIA_AUDIO, PulseMediaDirection.PULSE_MEDIA_OUTPUT);
            VideoHandle = PulseDeviceSession.pulse_device_session_create_video_handle(pulseInstance, PulseMediaContent.PULSE_MEDIA_CONTENT_SELFVIEW, 300, 300, 0xFF000000);
            BindHandle(VideoHandle);
            BindCamera();
        }
    }

    [ComImport]
    [Guid("63aad0b8-7c24-40ff-85a8-640d944cc325")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface ISwapChainPanelNative
    {
        [PreserveSig]
        uint SetSwapChain([In] IntPtr swapChain);
    }

    private void BindCamera()
    {
        // Connect the camera device to the session
        if (Cameras != null && Cameras.Length > 0)
        {
            PulseDevice camera = new PulseDevice
            {
                id = (uint)Cameras.First().Uid,
                name = Cameras.First().Name,
                media_direction = PulseMediaDirection.PULSE_MEDIA_INPUT,
                media_type = PulseMediaType.PULSE_MEDIA_VIDEO,
                is_default = Cameras.First().IsDefault ? 1 : 0,
            };

            var pulseError = PulseDeviceSession.pulse_device_session_connect_device(pulseInstance, camera, PulseMediaContent.PULSE_MEDIA_CONTENT_SELFVIEW);
            Debug.WriteLine("Connecting camera device : {0}, error code : {1}", camera.name, pulseError);
        }

    }

    private void BindHandle(/* IDXGISwapChain1 */ IntPtr swapChainPtr)
    {
        try
        {
             Debug.WriteLine("Binding video handle : 0x{0:X}", swapChainPtr);
            // Cast SwapChainPanel to IInspectable (IInspectable is the base interface for XAML objects in C++)
            var panelObj = Marshal.GetIUnknownForObject(cameraPreviewPanel!);

            // Query for ISwapChainPanelNative from the native object
            var guid = typeof(ISwapChainPanelNative).GUID;
            IntPtr panelPtr;
            Marshal.QueryInterface(panelObj, ref guid, out panelPtr);

            // Cast the returned pointer to ISwapChainPanelNative
            var panelNative = (ISwapChainPanelNative)Marshal.GetObjectForIUnknown(panelPtr);

            // Call SetSwapChain with your swap chain pointer
            panelNative.SetSwapChain(swapChainPtr);

            // Release the COM objects
            Marshal.Release(panelObj);
            Marshal.Release(panelPtr);
        }
        catch (Exception ex)
        {
            // this.logger.Error(ex.ToString());
        }
        finally
        {
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

    private bool CanContinue()
    {
        return VideoAlias != null && new AliasValidator().ValidateRegisteredAlias(VideoAlias);
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
                        this.dispatcherQueue.TryEnqueue(() => RegistrationProgress = $"{PulseError.pulse_strerror(err).ToString(Encoding.ASCII)}");
                        promise.TrySetResult(false);
                        //                        this.userContext = user_context; // read it in - and try and use it for the devices
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
