using System;

namespace Robust.Server.ServerStatus
{
    /// <summary>
    /// API for interacting with <c>SS14.Watchdog</c>.
    /// </summary>
    public interface IWatchdogApi
    {
        /// <summary>
        /// Raised when the game server should restart for an update.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This only indicates that the game server should restart as soon as possible without disruption,
        /// e.g. at the end of a round. It should not shut down immediately unless possible.
        /// </para>
        /// <para>
        /// This the same event as <see cref="RestartRequested"/>, but without additional data available such as reason.
        /// </para>
        /// </remarks>
        event Action UpdateReceived;

        /// <summary>
        /// Raised when the watchdog has indicated that the server should restart as soon as possible.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This only indicates that the game server should restart as soon as possible without disruption,
        /// e.g. at the end of a round. It should not shut down immediately unless possible.
        /// </para>
        /// <para>
        /// This the same event as <see cref="UpdateReceived"/>, but with additional data.
        /// </para>
        /// </remarks>
        event Action<RestartRequestedData> RestartRequested;
    }

    /// <summary>
    /// Engine-internal API for <see cref="IWatchdogApi"/>.
    /// </summary>
    internal interface IWatchdogApiInternal : IWatchdogApi
    {
        void Heartbeat();

        void Initialize();
    }

    /// <summary>
    /// Event data used by <see cref="IWatchdogApi.RestartRequested"/>.
    /// </summary>
    public sealed class RestartRequestedData
    {
        internal static readonly RestartRequestedData DefaultData = new(RestartRequestedReason.UpdateAvailable, null);

        /// <summary>
        /// Primary reason code for why the server should be restarted.
        /// </summary>
        public RestartRequestedReason Reason { get; }

        /// <summary>
        /// A message provided with additional information about the restart reason. Not always provided.
        /// </summary>
        public string? AdditionalMessage { get; }

        internal RestartRequestedData(RestartRequestedReason reason, string? additionalMessage)
        {
            Reason = reason;
            AdditionalMessage = additionalMessage;
        }
    }

    /// <summary>
    /// Primary reason codes for why the server should restart in <see cref="RestartRequestedData"/>.
    /// </summary>
    public enum RestartRequestedReason : byte
    {
        /// <summary>
        /// Restart reason does not fall in an existing category.
        /// </summary>
        Other = 0,

        /// <summary>
        /// The server should restart because an update is available.
        /// </summary>
        UpdateAvailable,

        /// <summary>
        /// The server should restart for maintenance.
        /// </summary>
        Maintenance,
    }
}
