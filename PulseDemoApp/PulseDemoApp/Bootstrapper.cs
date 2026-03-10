// <copyright file="Bootstrapper.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

namespace PulseDemoApp;

using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Pexip.Pulse.NativeEnums;
using Pexip.Pulse.NativeMethods;
using Windows.ApplicationModel.Activation;

public static class Bootstrapper
{
    public static bool HandleRedirection()
    {
        AppInstance appInstance = AppInstance.GetCurrent();
        AppActivationArguments activationArgs = appInstance.GetActivatedEventArgs();

        if (activationArgs.Kind == ExtendedActivationKind.Launch)
        {
            if (TryRedirectActivation(activationArgs))
            {
                return true;
            }

            appInstance.Activated += AppInstanceActivated;
        }
        else if (activationArgs.Kind == ExtendedActivationKind.Protocol)
        {
            if (TryRedirectActivation(activationArgs))
            {
                if (activationArgs.Data is IProtocolActivatedEventArgs protocolActivatedArgs &&
                    protocolActivatedArgs.Uri.Scheme == "pexip-auth") // SSO request
                {
                    HandleSSORequest((IProtocolActivatedEventArgs)activationArgs.Data);
                }

                return true;
            }

            appInstance.Activated += AppInstanceActivated;
        }

        return false;
    }

    private static void AppInstanceActivated(object? sender, AppActivationArguments e)
    {
        var app = (App)Application.Current;
        app.Window.DispatcherQueue.TryEnqueue(app.Window.Activate);
    }

    private static bool TryRedirectActivation(AppActivationArguments activationArgs)
    {
        AppInstance instance = AppInstance.FindOrRegisterForKey("PexipDemoApp");
        if (!instance.IsCurrent)
        {
            RedirectActivationTo(instance, activationArgs);
            return true;
        }

        return false;
    }

    private static void RedirectActivationTo(AppInstance appInstance, AppActivationArguments activationArgs)
    {
        using var signal = new Semaphore(0, 1);
        Task.Run(() =>
        {
            appInstance.RedirectActivationToAsync(activationArgs).AsTask().Wait();
            signal.Release();
        });
        signal.WaitOne();
    }

    private static void HandleSSORequest(IProtocolActivatedEventArgs activatedArgs)
    {
        var ssoToken = activatedArgs.Uri.ToString();
        PulseIPC.pulse_ipc_write_line("PulseWinClientSSOPipe", ssoToken);
    }
}
