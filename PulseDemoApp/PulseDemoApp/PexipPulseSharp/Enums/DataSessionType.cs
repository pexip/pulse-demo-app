// <copyright file="DataSessionType.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

namespace PulseDemoApp.PexipPulseSharp.Enums;

// FlagsAttribute requires values to be declared as powers of 2 in order to be treated bit-like
[Flags]
public enum DataSessionType
{
    Self = 1,
    Main = 2,
    Presentation = 4,
    Sharing = 8,
}
