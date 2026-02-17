// <copyright file="PulseMediaDevice.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

namespace PulseDemoApp.PexipPulseSharp;

using Pexip.Pulse.NativeStructs;

public sealed class PulseMediaDevice
{
    public PulseMediaDevice(IntPtr devicePtr, PulseDevice pulseDevice)
    {
        DevicePtr = devicePtr;
        PulseDevice = pulseDevice;
    }

    public IntPtr DevicePtr { get; }

    public PulseDevice PulseDevice { get; }

    public bool IsConnected { get; set; }
}
