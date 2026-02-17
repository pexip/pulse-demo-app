namespace PulseDemoApp.PexipPulseSharp;

using Pexip.Pulse.NativeDelegates;
using Pexip.Pulse.NativeEnums;
using Pexip.Pulse.NativeMethods;
using Pexip.Pulse.NativeStructs;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;

public sealed class PulseClient : IDisposable
{
    private readonly object @lock = new();
    private IntPtr instance;

    // connection events
    private PulseSSOProviderSelectionCallback ssoSelectionCallback;
    private PulseSSOProviderRequestCallback ssoRequestCallback;
    private PulseTLSDegradeApprovalCallback tlsDegradeApprovalCallback;
    private PulsePinCodeRequestCallback pinCodeRequestCallback;
    private PulseAsyncOperationResultCallback asyncOperationResultCallback;
    private PulseOperationProgressCallback operationProgressCallback;

    // registration events
    private PulseRegistrationStatusInfoCallback registrationStatusInfoCallback;
    private PulseRegistrationsEventIncomingCallback registrationEventIncomingCallback;
    private PulseAsyncOperationResultCallback registrationEventIncomingAsyncOperationResultCallback;
    private PulseOperationProgressCallback registrationEventIncomingOperationProgressCallback;
    private PulseRegistrationsEventIncomingCancelledCallback registrationEventIncomingCancelledCallback;

    // conference events
    private PulseConferenceStatusInfoCallback conferenceStatusInfoCallback;
    private PulseConferenceEventMessageReceivedCallback conferenceEventMessageReceivedCallback;
    private PulseConferenceEventConferenceUpdateCallback conferenceEventConferenceUpdateCallback;
    private PulseConferenceEventParticipantListUpdatedCallback conferenceEventParticipantListUpdateCallback;
    private PulseConferenceEventParticipantCreateCallback conferenceEventParticipantCreateCallback;
    private PulseConferenceEventParticipantDeleteCallback conferenceEventParticipantDeleteCallback;
    private PulseConferenceEventRemoteDisconnectCallback conferenceEventRemoteDisconnectCallback;
    private PulseConferenceEventPresentationStartCallback conferenceEventPresentationStartCallback;
    private PulseConferenceEventPresentationStopCallback conferenceEventPresentationStopCallback;
    private PulseConferenceEventLayoutCallback conferenceEventLayoutCallback;
    private PulseConferenceEventStageCallback conferenceEventStageCallback;
    private PulseConferenceAudioMixerListCallback conferenceEventAudioMixerListCallback;
    private PulseConferenceEventLiveCaptionsCallback conferenceEventLiveCaptionsCallback;

    // breakout rooms events
    private PulseBreakoutRoomPreTransferCallback breakoutRoomPreTransferCallback;
    private PulseBreakoutRoomPostTransferCallback breakoutRoomPostTransferCallback;
    private PulseBreakoutRoomTransferCancelledCallback breakoutRoomTransferCancelledCallback;
    private PulseBreakoutRoomCreatedCallback breakoutRoomCreatedCallback;
    private PulseBreakoutRoomDestroyedCallback breakoutRoomDestroyedCallback;

    // device events
    private PulseDeviceErrorFunc deviceErrorCallback;
    private PulseDeviceListChangeFunc deviceListChangeCallback;
    private PulseDeviceAudioLevelFunc deviceAudioLevelCallback;
    private PulseAudioUnmuteApprovalCallback audioUnmuteApprovalCallback;
    private PulseAudioMuteStateChangedCallback audioMuteStateChangedCallback;

    // misc events
    private PulseVersionCallback versionCallback;
    private PulseStorageSetDataCallback storageSetCallback;
    private PulseStorageGetDataCallback storageGetCallback;


    public void Dispose()
    {
        // Dispose(false);
    }

    #region Connection Events


    public delegate Task<TReturn> AsyncEventHandler<TReturn, TEventArgs>(TEventArgs e)
        where TEventArgs : EventArgs;

    internal event AsyncEventHandler<bool, SsoProviderSelectedEventArgs> SsoRegistrationProviderSelected;

    internal event AsyncEventHandler<bool, SsoProviderSelectedEventArgs> SsoRegistrationProviderReselected;

    internal event AsyncEventHandler<bool, SsoProviderSelectedEventArgs> SsoConferenceProviderSelected;

    internal event AsyncEventHandler<string?, SsoProvidersSelectedEventArgs> SsoRegistrationProvidersSelected;

    internal event AsyncEventHandler<string?, SsoProvidersSelectedEventArgs> SsoRegistrationProvidersReselected;

    internal event AsyncEventHandler<string?, SsoProvidersSelectedEventArgs> SsoConferenceProvidersSelected;

    internal event AsyncEventHandler<string?, SsoTokenRequestEventArgs> SSOTokenRequest;

    internal event AsyncEventHandler<bool, TlsApprovalRequestedEventArgs> TlsApprovalRequested;

    internal event AsyncEventHandler<PinCodeResponse?, PinCodeRequestedEventArgs> PinCodeRequested;

    internal event EventHandler<ConnectionProgressedEventArgs> ConnectionProgressed;

    internal event EventHandler<ConnectionAbortedEventArgs> ConnectionAborted;

    internal event EventHandler<RegistrationAbortedEventArgs> RegistrationAborted;

    internal event EventHandler<ConnectionFailedEventArgs> ConnectionFailed;

    internal event EventHandler<RegistrationFailedEventArgs> RegistrationFailed;

    internal event EventHandler<OperationFailedEventArgs> OperationFailed;

    #endregion

    #region Registration Events

    internal event EventHandler<RegistrationChangedEventArgs> RegistrationChanged;

    internal event EventHandler<RegistrationProgressedEventArgs> RegistrationProgressed;

    internal event AsyncEventHandler<bool?, CallIncomingEventArgs> CallIncoming;

    internal event AsyncEventHandler<bool, CallAcceptedEventArgs> CallAccepted;

    internal event AsyncEventHandler<CallDeclinedEventArgs> CallDeclined;

    internal event AsyncEventHandler<CallEstablishedEventArgs> CallEstablished;

    internal event AsyncEventHandler<CallMissedEventArgs> CallMissed;

    #endregion

    #region Conference Events

    internal event EventHandler<ConnectionChangedEventArgs> ConnectionChanged;

    internal event EventHandler<ConferenceUpdatedEventArgs> ConferenceUpdated;

    internal event EventHandler<LayoutUpdatedEventArgs> LayoutUpdated;

    internal event EventHandler<MessageReceivedEventArgs> MessageReceived;

    internal event EventHandler<RemoteDisconnectedEventArgs> RemoteDisconnected;

    internal event EventHandler<ConnectionRejectedEventArgs> ConnectionRejected;

    internal event EventHandler<ConnectionUnacknowledgedEventArgs> ConnectionUnacknowledged;

    internal event EventHandler<PresentationStartedEventArgs> PresentationStarted;

    internal event EventHandler<PresentationStoppedEventArgs> PresentationStopped;

    internal event AsyncEventHandler<ParticipantsUpdatedEventArgs> ParticipantsUpdated;

    internal event AsyncEventHandler<SpeakerParticipantsUpdatedEventArgs> SpeakerParticipantsUpdated;

    internal event AsyncEventHandler<ParticipantRefreshedEventArgs> ParticipantRefreshed;

    internal event AsyncEventHandler<ParticipantCreatedEventArgs> ParticipantCreated;

    internal event AsyncEventHandler<ParticipantDeletedEventArgs> ParticipantDeleted;

    #endregion

    #region Breakout Room Events

    internal event AsyncEventHandler<RoomPreTransferredEventArgs> RoomPreTransferred;

    internal event EventHandler<RoomPostTransferredEventArgs> RoomPostTransferred;

    #endregion

    #region Device Events

    internal event EventHandler<DeviceErrorEventArgs> DeviceError;

    internal event EventHandler<DevicesUpdatedEventArgs> DevicesUpdated;

    //internal event EventHandler<DeviceAudioLevelChangedEventArgs> DeviceAudioLevelChanged;

    //internal event EventHandler<DeviceAudioLevelsChangedEventArgs> DeviceAudioLevelsChanged;

    internal event AsyncEventHandler<bool, AudioUnmuteApprovalRequestedEventArgs> AudioUnmuteApprovalRequested;

    internal event EventHandler<AudioMuteStateChangedEventArgs> AudioMuteStateChanged;

    #endregion

    #region Misc Events

//    internal event EventHandler<bool, VersionEventArgs> Version;

    internal event AsyncEventHandler<SecureString?, StorageValueGetEventArgs> StorageValueGet;

    internal event AsyncEventHandler<bool, StorageValueSetEventArgs> StorageValueSet;

    internal event EventHandler<ActionExecutedEventArgs> ActionExecuted;

    #endregion

    internal List<PulseMediaDevice> MediaDevices { get; } = new();

}
