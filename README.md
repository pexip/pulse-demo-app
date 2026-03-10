<img align="left" width="116" height="116" src="https://docs.pexip.com/Resources/Images/generic/pexip_favicon.png" />

# Pexip Pulse Demo App

<br/>

A .NET getting-started sample application demonstrating how to integrate the **Pexip Pulse SDK** ([`Pexip.Pulse`](https://www.pexip.com/) NuGet package) into a Windows desktop app. It is built with .NET 8, WinUI 3, and packaged with MSIX.

The demo covers the full conferencing lifecycle - SSO-based registration, device selection, joining a video call, and graceful disconnect - so that you can use it as a reference when building your own application on top of the Pexip Infinity platform.

## Technologies

| Package / Framework | Purpose |
|---|---|
| [`Pexip.Pulse`](https://www.pexip.com/) | **Pexip Pulse SDK** - core conferencing engine |
| [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) | Target runtime |
| [WinUI 3](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/) | Modern Windows UI framework |
| [Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/) | WinUI 3 host |
| [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) | MVVM helpers (`ObservableObject`, `RelayCommand`) |
| [MSIX](https://learn.microsoft.com/en-us/windows/msix/overview) | Packaging & distribution |

<hr>

## Prerequisites

- **Windows 10** (build 17763 / 1809 or later) or **Windows 11**
- [Visual Studio 2022](https://visualstudio.microsoft.com/) with the **Windows application development** workload  
  *(includes the .NET 8 SDK, WinUI 3, and Windows App SDK tools)*
- Access to the **Pexip Artifactory NuGet feed** (see [NuGet Setup](#nuget-setup) below)

<hr>

## Getting Started

```bash
git clone https://github.com/pexip/pulse-demo-app.git
cd pulse-demo-app/PulseDemoApp
```

Open `PulseDemoApp.sln` in Visual Studio 2022, select the desired platform (`x64` recommended), and press **F5** to build and run.

<hr>

## NuGet Setup

The Pexip Pulse SDK is distributed as the `Pexip.Pulse` NuGet package from a **private Pexip Artifactory feed**. The repository already ships a `nuget.config` that configures both the public nuget.org feed and the Pexip feed:

```xml
<!-- PulseDemoApp/nuget.config -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <!-- Public packages (Microsoft.*, CommunityToolkit.*) -->
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <!-- Pexip private feed - hosts Pexip.* packages -->
    <add key="MediaMain" value="https://artifactory.geo.ci.pexip.com/artifactory/api/nuget/v3/media-nuget-main/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="Microsoft.*" />
      <package pattern="CommunityToolkit.*" />
    </packageSource>
    <!-- Route all Pexip.* packages to the private feed -->
    <packageSource key="MediaMain">
      <package pattern="Pexip.*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

The SDK is then referenced in the project file like any other NuGet package:

```xml
<!-- PulseDemoApp/PulseDemoApp/PulseDemoApp.csproj -->
<ItemGroup>
  <PackageReference Include="Pexip.Pulse" Version="1.0.16785+60474b0bf70b" />
</ItemGroup>
```

> **Note:** You must have valid credentials for the Pexip Artifactory feed. Contact your Pexip account representative if you need access.

<hr>

## Integration Guide

The sections below walk through each major feature shown in the demo, with references to the relevant source files.

### 1. Namespaces

After adding `Pexip.Pulse` to your project, import the SDK namespaces:

```csharp
using Pexip.Pulse.NativeEnums;
using Pexip.Pulse.NativeMethods;
using Pexip.Pulse.NativeStructs;
```

### 2. Creating and Destroying a Pulse Instance

All SDK calls require a `pulseInstance` handle. Create it once at startup and free it when the application exits.

```csharp
// Create once - keep for the lifetime of the component
IntPtr pulseInstance = PulseConnect.pulse_new();

// Free when done (e.g. in Dispose())
PulseConnect.pulse_free(pulseInstance);
```

See [`MainViewModel.cs`](PulseDemoApp/PulseDemoApp/MainViewModel.cs) for the full lifecycle pattern.

### 3. Enumerating Devices

Query available cameras, microphones, and speakers before joining a call:

```csharp
// Get all video input devices (cameras)
PulseDevices.pulse_device_iterator_new(
    pulseInstance,
    PulseMediaType.PULSE_MEDIA_VIDEO,
    PulseMediaDirection.PULSE_MEDIA_INPUT,
    out IntPtr iterator);

var cameras = new List<PulseDevice>();
PulseDevices.pulse_device_iterator_foreach(
    iterator,
    (device, _) => cameras.Add(device),
    userContext: 0);

PulseDevices.pulse_device_iterator_free(iterator);
```

The same pattern works for audio input (`PULSE_MEDIA_AUDIO` / `PULSE_MEDIA_INPUT`) and audio output (`PULSE_MEDIA_AUDIO` / `PULSE_MEDIA_OUTPUT`).

### 4. SSO Registration

Register the user's address against the Pexip Infinity platform using SSO:

```csharp
// 1. Configure the registration state callback
PulseOptions.pulse_options_set_registration_state_callback(
    pulseInstance,
    new PulseRegistrationStatusCallbackConfig
    {
        func = (status_info, _) =>
        {
            if (status_info.status == PulseConnectionStatus.PULSE_CONNECTION_STATUS_CONNECTED)
            {
                // Registration succeeded - enable the next step in the UI
            }
        }
    });

// 2. Configure SSO provider callbacks
PulseOptions.pulse_options_set_sso_provider_callbacks(
    pulseInstance,
    new PulseSSOProviderCallbackConfig
    {
        selection_callback = (ref PulseSSOProviderList list, IntPtr _) =>
        {
            // Accept the first SSO provider
            return 0;
        },
        request_callback = (PulseSSOProviderRequest request, PulseSSOProviderSetToken setToken, IntPtr _) =>
        {
            // Open the browser and collect the token (e.g. via a named pipe)
            string? token = RequestSSOToken(new Uri(request.url));
            return token is string t && setToken.func(setToken.context, t);
        },
    });

// 3. Start the async registration
PulseRegistration.pulse_register_async(
    pulseInstance,
    new PulseRegistrationRequest
    {
        alias   = "user@example.com",
        host    = "example.com",
        use_sso = true,
    },
    resultCallback: ...,
    progressCallback: ...);
```

See [`Bootstrapper.cs`](PulseDemoApp/PulseDemoApp/Bootstrapper.cs) for the companion SSO token delivery implementation using a named pipe (`PulseWinClientSSOPipe`).

### 5. Connecting Devices

After the user picks a device, connect it to the session:

```csharp
// Connect selected camera
PulseDeviceSession.pulse_device_session_connect_device(
    pulseInstance,
    selectedCamera,
    PulseMediaContent.PULSE_MEDIA_CONTENT_MAIN);

// Connect selected microphone / speaker with the same call
PulseDeviceSession.pulse_device_session_connect_device(
    pulseInstance,
    selectedMicrophone,
    PulseMediaContent.PULSE_MEDIA_CONTENT_MAIN);
```

### 6. Video Handles (Self-View & Main Video)

Create a video handle before joining so the SDK can render into it:

```csharp
// Create a video handle for self-view
IntPtr selfVideoHandle = PulseDeviceSession.pulse_device_session_create_video_handle(
    pulseInstance,
    PulseMediaContent.PULSE_MEDIA_CONTENT_SELFVIEW,
    width: (int)selfViewPanel.ActualWidth,
    height: (int)selfViewPanel.ActualHeight,
    backgroundColor: 0xFF212121);

// Resize when the container changes size
PulseDeviceSession.pulse_device_session_resize_video_handle(
    pulseInstance, selfVideoHandle, newWidth, newHeight);

// Release when done
PulseDeviceSession.pulse_device_session_release_video_handle(
    pulseInstance, selfVideoHandle);
```

The [`VideoView`](PulseDemoApp/PulseDemoApp/UserControls/VideoView.xaml.cs) user control in the demo shows how to bind an SDK video handle to a WinUI 3 `SwapChainPanel` for DirectX rendering.

### 7. Joining a Conference

```csharp
// Set the conference state callback first
PulseOptions.pulse_options_set_conference_state_callback(
    pulseInstance,
    new PulseConferenceStatusCallbackConfig
    {
        func = (status_info, _) =>
        {
            if (status_info.status == PulseConnectionStatus.PULSE_CONNECTION_STATUS_CONNECTED)
            {
                // Joined - show the conference UI
            }
        },
    });

// Then connect
PulseConnect.pulse_connect_with_rest_async(
    pulseInstance,
    new PulseRestConnectionConfig
    {
        server_address  = "example.com",
        display_name    = "Alice",
        conference_name = "alice@example.com",
        pin_code        = "1234",   // optional
    },
    resultCallback:   ...,
    progressCallback: ...);
```

### 8. Leaving a Conference

```csharp
PulseConnect.pulse_disconnect_async(
    pulseInstance,
    resultCallback:   ...,
    progressCallback: ...);

// Synchronous variant (e.g. in Dispose)
if (PulseConnect.pulse_is_connected(pulseInstance))
    PulseConnect.pulse_disconnect(pulseInstance, default);
```

### 9. Error Handling

Every SDK method returns a `PulseErrorType`. Check for `PULSE_SUCCESS` and use `pulse_strerror` to obtain a human-readable message:

```csharp
PulseErrorType err = PulseDeviceSession.pulse_device_session_connect_device(...);
if (err != PulseErrorType.PULSE_SUCCESS)
{
    string message = PulseError.pulse_strerror(err).ToString(Encoding.ASCII);
    // Log or surface the message in your UI
}
```

<hr>

## Application Architecture

```
PulseDemoApp/
├── App.xaml(.cs)           - WinUI 3 application entry, window setup
├── MainWindow.xaml(.cs)    - Top-level window
├── MainPage.xaml(.cs)      - Page host; injects MainViewModel
├── MainViewModel.cs        - All SDK interactions via MVVM commands
├── Bootstrapper.cs         - Single-instance + SSO protocol handling
├── UserControls/
│   └── VideoView.xaml(.cs) - SwapChainPanel wrapper for SDK video handles
└── Utilities/
    ├── AliasValidator.cs   - Input validation helpers
    └── Extensions.cs       - Native ↔ managed interop helpers
```

The UI is divided into four progressive cards:

| Step | Card | SDK actions |
|---|---|---|
| 1 | **Registration** | `pulse_register_async` + SSO callbacks |
| 2 | **Connection** | Validate alias / display name (no SDK call) |
| 3 | **Join** | Device enumeration, `pulse_device_session_connect_device`, video handles |
| 4 | **Conference** | `pulse_connect_with_rest_async`, `pulse_disconnect_async` |

<hr>

## Building & Packaging

```bash
# Restore NuGet packages (requires access to the Pexip Artifactory feed)
dotnet restore PulseDemoApp/PulseDemoApp.sln

# Build for x64
dotnet build PulseDemoApp/PulseDemoApp.sln -c Release -p:Platform=x64
```

To produce an MSIX installer, open the solution in Visual Studio 2022, right-click the **PulseDemoApp (Package)** project, and choose **Publish → Create App Packages**.

<hr>

## License

Copyright © Pexip. All rights reserved.