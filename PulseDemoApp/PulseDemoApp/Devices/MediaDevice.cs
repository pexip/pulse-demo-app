using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseDemoApp.Devices
{
    public class MediaDevice : ObservableObject
    {
        public MediaDevice(uint uid, string name, bool isDefault, bool isConnected)
        {
            Uid = uid;
            Name = name;
            IsDefault = isDefault;
            IsConnected = isConnected;
        }

        public uint Uid { get; }

        public string Name { get; }

        public bool IsDefault { get; }

        public bool IsConnected { get; }
    }
}