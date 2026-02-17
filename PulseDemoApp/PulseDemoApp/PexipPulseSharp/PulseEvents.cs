// <copyright file="PulseEvents.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

namespace PulseDemoApp.PexipPulseSharp;

using System;
using System.Security;

using Pexip.Pulse.NativeEnums;
// using Pexip.PulseSharp.Api.Domain;
// using Pexip.PulseSharp.Api.Domain.Enums;

#pragma warning disable SA1124,SA1402,SA1649,CA1819

public delegate void EventHandler<TEventArgs>(TEventArgs e)
    where TEventArgs : EventArgs;

public delegate TReturn EventHandler<TReturn, TEventArgs>(TEventArgs e)
    where TEventArgs : EventArgs;

public delegate Task AsyncEventHandler<TEventArgs>(TEventArgs e)
    where TEventArgs : EventArgs;

public delegate Task<TReturn> AsyncEventHandler<TReturn, TEventArgs>(TEventArgs e)
    where TEventArgs : EventArgs;

#region Connection Event Args

public class SsoProviderSelectedEventArgs : EventArgs
{
    public SsoProviderSelectedEventArgs(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

public class SsoProvidersSelectedEventArgs : EventArgs
{
    public SsoProvidersSelectedEventArgs(IList<string> names)
    {
        Names = names;
    }

    public IList<string> Names { get; }
}

public class SsoTokenRequestEventArgs : EventArgs
{
    public SsoTokenRequestEventArgs(Uri url, bool reconnecting)
    {
        Url = url;
        Reconnecting = reconnecting;
    }

    public Uri Url { get; }

    public bool Reconnecting { get; }
}

public class TlsApprovalRequestedEventArgs : EventArgs
{
    public TlsApprovalRequestedEventArgs(string host, string reason)
    {
        Host = host;
        Reason = reason;
    }

    public string Host { get; }

    public string Reason { get; }
}

public class PinCodeRequestedEventArgs : EventArgs
{
    public PinCodeRequestedEventArgs(bool pinRequired)
    {
        PinRequired = pinRequired;
    }

    public bool PinRequired { get; }
}

public class ConnectionChangedEventArgs : EventArgs
{
    public ConnectionChangedEventArgs(ConnectionState state, ConnectionType type)
    {
        State = state;
        Type = type;
    }

    public ConnectionState State { get; }

    public ConnectionType Type { get; }
}

public class ConnectionProgressedEventArgs : EventArgs
{
    public ConnectionProgressedEventArgs(float value, string description)
    {
        Value = value;
        Description = description;
    }

    public float Value { get; }

    public string Description { get; }
}

public class ConnectionAbortedEventArgs : EventArgs
{
    public ConnectionAbortedEventArgs(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }

    public string ErrorMessage { get; }
}

public class RegistrationAbortedEventArgs : EventArgs
{
    public RegistrationAbortedEventArgs(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }

    public string ErrorMessage { get; }
}

public class ConnectionFailedEventArgs : EventArgs
{
    public ConnectionFailedEventArgs(PulseErrorType errorType, string errorMessage)
    {
        ErrorType = errorType;
        ErrorMessage = errorMessage;
    }

    public PulseErrorType ErrorType { get; }

    public string ErrorMessage { get; }
}

public class RegistrationFailedEventArgs : EventArgs
{
    public RegistrationFailedEventArgs(PulseErrorType errorType, string errorMessage)
    {
        ErrorType = errorType;
        ErrorMessage = errorMessage;
    }

    public PulseErrorType ErrorType { get; }

    public string ErrorMessage { get; }
}

public class OperationFailedEventArgs : EventArgs
{
    public OperationFailedEventArgs(PulseErrorType errorType, string errorMessage)
    {
        ErrorType = errorType;
        ErrorMessage = errorMessage;
    }

    public PulseErrorType ErrorType { get; }

    public string ErrorMessage { get; }
}

#endregion

#region Registration Event Args

public class RegistrationChangedEventArgs : EventArgs
{
    public RegistrationChangedEventArgs(ConnectionState state, ulong nextReconnectSeconds)
    {
        State = state;
        NextReconnectSeconds = nextReconnectSeconds;
    }

    public ConnectionState State { get; }

    public ulong NextReconnectSeconds { get; }
}

public class RegistrationProgressedEventArgs : EventArgs
{
    public RegistrationProgressedEventArgs(float value, string description)
    {
        Value = value;
        Description = description;
    }

    public float Value { get; }

    public string Description { get; }
}

public class CallIncomingEventArgs : EventArgs
{
    public CallIncomingEventArgs(IncomingCall incomingCall)
    {
        IncomingCall = incomingCall;
    }

    public IncomingCall IncomingCall { get; }
}

public class CallAcceptedEventArgs : EventArgs
{
    public CallAcceptedEventArgs(IncomingCall incomingCall)
    {
        IncomingCall = incomingCall;
    }

    public IncomingCall IncomingCall { get; }
}

public class CallDeclinedEventArgs : EventArgs
{
    public CallDeclinedEventArgs(IncomingCall incomingCall)
    {
        IncomingCall = incomingCall;
    }

    public IncomingCall IncomingCall { get; }
}

public class CallEstablishedEventArgs : EventArgs
{
    public CallEstablishedEventArgs(IncomingCall incomingCall)
    {
        IncomingCall = incomingCall;
    }

    public IncomingCall IncomingCall { get; }
}

public class CallMissedEventArgs : EventArgs
{
    public CallMissedEventArgs(IncomingCall incomingCall)
    {
        IncomingCall = incomingCall;
    }

    public IncomingCall IncomingCall { get; }
}

#endregion

#region Conference Event Args

public class ConferenceUpdatedEventArgs : EventArgs
{
    public ConferenceUpdatedEventArgs(ConferenceStatus status)
    {
        Status = status;
    }

    public ConferenceStatus Status { get; }
}

public class LayoutUpdatedEventArgs : EventArgs
{
    public LayoutUpdatedEventArgs(ConferenceLayout layout)
    {
        Layout = layout;
    }

    public ConferenceLayout Layout { get; }
}

public class MessageReceivedEventArgs : EventArgs
{
    public MessageReceivedEventArgs(Guid uid, string sender, string message, string mimeType, bool direct)
    {
        SenderUid = uid;
        SenderName = sender;
        Message = message;
        MimeType = mimeType;
        Direct = direct;
    }

    public Guid SenderUid { get; }

    public string SenderName { get; }

    public string Message { get; }

    public string MimeType { get; }

    public bool Direct { get; }
}

public class RemoteDisconnectedEventArgs : EventArgs
{
    public RemoteDisconnectedEventArgs(string reason)
    {
        Reason = reason;
    }

    public string Reason { get; }
}

public class ConnectionRejectedEventArgs : EventArgs
{
    public ConnectionRejectedEventArgs(string reason)
    {
        Reason = reason;
    }

    public string Reason { get; }
}

public class ConnectionUnacknowledgedEventArgs : EventArgs
{
    public ConnectionUnacknowledgedEventArgs(string reason)
    {
        Reason = reason;
    }

    public string Reason { get; }
}

public class PresentationStartedEventArgs : EventArgs
{
    public PresentationStartedEventArgs(string presenterName)
    {
        PresenterName = presenterName;
    }

    public string PresenterName { get; }
}

public class PresentationStoppedEventArgs : EventArgs
{
    public PresentationStoppedEventArgs()
    {
    }
}

public class ParticipantsUpdatedEventArgs : EventArgs
{
    public ParticipantsUpdatedEventArgs(IList<ConferenceParticipant> participants)
    {
        Participants = participants;
    }

    public IList<ConferenceParticipant> Participants { get; }
}

public class SpeakerParticipantsUpdatedEventArgs : EventArgs
{
    public SpeakerParticipantsUpdatedEventArgs(IList<SpeakerParticipant> participants)
    {
        Participants = participants;
    }

    public IList<SpeakerParticipant> Participants { get; }
}

public class ParticipantRefreshedEventArgs : EventArgs
{
    public ParticipantRefreshedEventArgs(ConferenceParticipant participant)
    {
        Participant = participant;
    }

    public ConferenceParticipant Participant { get; }
}

public class ParticipantCreatedEventArgs : EventArgs
{
    public ParticipantCreatedEventArgs(Guid uid)
    {
        Uid = uid;
    }

    public Guid Uid { get; }
}

public class ParticipantDeletedEventArgs : EventArgs
{
    public ParticipantDeletedEventArgs(Guid uid)
    {
        Uid = uid;
    }

    public Guid Uid { get; }
}

#endregion

#region Breakout Room Event Args

public class RoomPreTransferredEventArgs : EventArgs
{
    public RoomPreTransferredEventArgs(string roomName)
    {
        RoomName = roomName;
    }

    public string RoomName { get; }
}

public class RoomPostTransferredEventArgs : EventArgs
{
}

#endregion

#region Data Session Events

//public class VideoFrameArrivedEventArgs : EventArgs
//{
//    public VideoFrameArrivedEventArgs(VideoFrame frame)
//    {
//        Frame = frame;
//    }

//    public VideoFrame Frame { get; }
//}

#endregion

#region Device Events

public class DeviceErrorEventArgs : EventArgs
{
    public DeviceErrorEventArgs(uint deviceUid, PulseErrorType error)
    {
        DeviceUid = deviceUid;
        Error = error;
    }

    public uint DeviceUid { get; }

    public PulseErrorType Error { get; }
}

public class DevicesUpdatedEventArgs : EventArgs
{
    public DevicesUpdatedEventArgs()
    {
    }
}

//public class DeviceAudioLevelChangedEventArgs : EventArgs
//{
//    public DeviceAudioLevelChangedEventArgs(AudioActivity level)
//    {
//        Level = level;
//    }

//    public AudioActivity Level { get; }
// }

//public class DeviceAudioLevelsChangedEventArgs : EventArgs
//{
//    public DeviceAudioLevelsChangedEventArgs(IList<AudioActivity> levels)
//    {
//        Levels = levels;
//    }

//    public IList<AudioActivity> Levels { get; }
//}

//public class AudioUnmuteApprovalRequestedEventArgs : EventArgs
//{
//    public AudioUnmuteApprovalRequestedEventArgs()
//    {
//    }
//}

//public class AudioMuteStateChangedEventArgs : EventArgs
//{
//    public AudioMuteStateChangedEventArgs(bool clientMuted, bool serverMuted)
//    {
//        ClientMuted = clientMuted;
//        ServerMuted = serverMuted;
//    }

//    public bool ClientMuted { get; }

//    public bool ServerMuted { get; }
//}

#endregion

#region Misc Events

//public class VersionEventArgs : EventArgs
//{
//    public VersionEventArgs(InfinityVersion serverVersion)
//    {
//        ServerVersion = serverVersion;
//    }

//    public InfinityVersion ServerVersion { get; }
//}

public class StorageValueGetEventArgs : EventArgs
{
    public StorageValueGetEventArgs(string key)
    {
        Key = key;
    }

    public string Key { get; }
}

public class StorageValueSetEventArgs : EventArgs
{
    public StorageValueSetEventArgs(string key, SecureString? value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; }

    public SecureString? Value { get; }
}

public class ActionExecutedEventArgs : EventArgs
{
    public ActionExecutedEventArgs(string action, IDictionary<string, object?> arguments, object? result)
    {
        Action = action;
        Arguments = arguments;
        Result = result;
    }

    public string Action { get; }

    public IDictionary<string, object?> Arguments { get; }

    public object? Result { get; }
}

#endregion
