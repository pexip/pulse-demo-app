using Pexip.Pulse.NativeEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseDemoApp.Devices
{
    public enum DeviceState
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
    }
}