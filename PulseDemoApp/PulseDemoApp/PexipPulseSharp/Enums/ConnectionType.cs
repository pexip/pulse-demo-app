// <copyright file="ConnectionType.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

namespace PulseDemoApp.PexipPulseSharp.Enums;

public enum ConnectionType
{
    Unknown,
    Connecting,
    WaitingRoom,
    Ivr,
    Conference,
    Lecture,
    Gateway,
    TestCall,
}
