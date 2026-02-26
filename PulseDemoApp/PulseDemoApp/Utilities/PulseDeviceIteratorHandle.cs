// <copyright file="PulseDeviceIteratorHandle.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

using Pexip.Pulse.NativeMethods;

namespace PulseDemoApp.Utilities
{
    public sealed class PulseDeviceIteratorHandle : IDisposable
    {
        public PulseDeviceIteratorHandle(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; private set; }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                PulseDevices.pulse_device_iterator_free(Handle);
                Handle = IntPtr.Zero;
            }
        }
    }
}
