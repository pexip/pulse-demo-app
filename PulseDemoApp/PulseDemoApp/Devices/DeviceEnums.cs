// <copyright file="DeviceEnums.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

namespace PulseDemoApp.Devices
{
    public enum DeviceEnums
    {
        Active,
        Muted,
        Revoked,
        Busy,
        Error,
    }
    public enum MediaDirection
    {
        PULSE_MEDIA_INPUT,
        PULSE_MEDIA_OUTPUT
    }

    public enum MediaType
    {
        PULSE_MEDIA_AUDIO,
        PULSE_MEDIA_VIDEO
    }
}
