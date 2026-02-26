// <copyright file="MediaDevice.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

using Pexip.Pulse.NativeEnums;
using Pexip.Pulse.NativeStructs;

namespace PulseDemoApp.Devices
{
    public class MediaDevice : ObservableObject
    {
        public MediaDevice(uint uid, string name, PulseMediaType mediaType, PulseMediaDirection pulseMediaDirection, int onList, bool isDefault, bool isConnected)
        {
            Uid = uid;
            Name = name;
            MediaType = mediaType;
            MediaDirection = pulseMediaDirection;
            OnList = onList;
            IsDefault = isDefault;
            IsConnected = isConnected;
        }

        public uint Uid { get; }

        public string Name { get; }

        public PulseMediaType MediaType;

        public PulseMediaDirection MediaDirection;

        public int OnList;

        public bool IsDefault { get; }

        public bool IsConnected { get; set; }

        public PulseDevice ToPulseDevice()
        {
            return new PulseDevice
            {
                id = Uid,
                name = Name,
                media_type = MediaType,
                media_direction = MediaDirection,
                on_list = OnList,
                is_default = IsDefault ? 1 : 0,
            };
        }
    }
}