// <copyright file="PinCodeResponse.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

namespace PulseDemoApp.PexipPulseSharp;

public class PinCodeResponse
{
    public PinCodeResponse(bool accept, bool abort, string? pinCode)
    {
        Accept = accept;
        Abort = abort;
        PinCode = pinCode;
    }

    public bool Accept { get; }

    public bool Abort { get; }

    public string? PinCode { get; }
}
