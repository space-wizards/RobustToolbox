using System;
using System.IO;
using System.Threading.Tasks;

namespace Robust.Shared.Network.Transfer;

/// <summary>
/// API for high-bandwidth asynchronous data transfers between client and server.
/// </summary>
/// <remarks>
/// <para>
/// Due to technical limitations of our normal networking layer, it is not possible to send high volumes of traffic
/// over it. <see cref="ITransferManager"/> can avoid this limitation by using a secondary WebSocket-based channel
/// for these transfers.
/// </para>
/// <para>
/// The high-bandwidth channel is not available by default and must be configured by the server via
/// <see cref="CVars.TransferHttp"/>. If enabled, clients will connect to the channel when connecting to the server.
/// </para>
/// <para>
/// While the methods on <see cref="ITransferManager"/> themselves are not thread safe,
/// it is safe to read and write from created streams from multiple threads (one per stream).
/// </para>
/// </remarks>
[NotContentImplementable]
public interface ITransferManager
{
    /// <summary>
    /// Start a transfer to a channel.
    /// </summary>
    /// <param name="channel">The channel to send data to.</param>
    /// <param name="startInfo">Additional info to start the transfer.</param>
    /// <returns>
    /// A stream that can be written to send data.
    /// This stream may employ buffering, flush or close the stream to ensure data is sent immediately.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the provided transfer key was not registered with <see cref="RegisterTransferMessage"/>.
    /// </exception>
    Stream StartTransfer(INetChannel channel, TransferStartInfo startInfo);

    /// <summary>
    /// Register a transfer stream key for sending and/or receiving.
    /// </summary>
    /// <param name="key">The name of the stream to register.</param>
    /// <param name="rxCallback">
    /// Callback to be run when the stream is received.
    /// If null, this stream may not be received on this side of the network.
    /// </param>
    /// <param name="accept">
    /// Which sides of the network this stream is accepted on.
    /// Useful in shared code where passing <paramref name="rxCallback"/> separately may be annoying.
    /// </param>
    void RegisterTransferMessage(
        string key,
        Action<TransferReceivedEvent>? rxCallback = null,
        NetMessageAccept accept = NetMessageAccept.Both);

    // Engine API.

    internal void Initialize();
    internal void FrameUpdate();
    internal Task ServerHandshake(INetChannel channel);
    internal event Action ClientHandshakeComplete;
}

/// <summary>
/// Extension methods for <see cref="ITransferManager"/>.
/// </summary>
public static class TransferManagerExt
{
    /// <summary>
    /// Start a transfer to a channel.
    /// </summary>
    /// <param name="manager">The manager to start the transfer with.</param>
    /// <param name="channel">The channel to send data to.</param>
    /// <param name="key">Key to start transfer for.</param>
    /// <returns>
    /// A stream that can be written to send data.
    /// This stream may employ buffering, flush or close the stream to ensure data is sent immediately.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the provided transfer key was not registered with <see cref="ITransferManager.RegisterTransferMessage"/>.
    /// </exception>
    public static Stream StartTransfer(this ITransferManager manager, INetChannel channel, string key)
    {
        return manager.StartTransfer(channel,
            new TransferStartInfo
            {
                MessageKey = key
            });
    }
}

/// <summary>
/// Information used to start a transfer stream.
/// </summary>
public sealed class TransferStartInfo
{
    /// <summary>
    /// The key to start the transfer for. This uniquely identifies a "use case" and must be registered in advance.
    /// </summary>
    public required string MessageKey;
}

/// <summary>
/// Event data raised when a new transfer stream is received.
/// </summary>
public sealed class TransferReceivedEvent
{
    /// <summary>
    /// The key being transferred for.
    /// </summary>
    public readonly string Key;

    /// <summary>
    /// A stream that can be used to read the received data.
    /// </summary>
    /// <remarks>
    /// Users should drain this stream as quickly as possible, as failing to do so may stall the entire transfer system.
    /// </remarks>
    public readonly Stream DataStream;

    /// <summary>
    /// The net channel that is sending the data.
    /// </summary>
    public readonly INetChannel Channel;

    internal TransferReceivedEvent(string key, INetChannel channel, Stream stream)
    {
        Key = key;
        DataStream = stream;
        Channel = channel;
    }
}
