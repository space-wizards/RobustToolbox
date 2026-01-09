using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Asynchronous;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Replays;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;

namespace Robust.Shared.Upload;

/// <summary>
///     Manager that allows resources to be added at runtime by admins.
///     They will be sent to all clients automatically.
/// </summary>
public abstract class SharedNetworkResourceManager : IDisposable, IPostInjectInit
{
    /// <summary>
    /// Transfer key for client -> server uploads by privileged clients.
    /// </summary>
    internal const string TransferKeyNetworkUpload = "TransferKeyNetworkUpload";

    /// <summary>
    /// Transfer key for server -> client downloads
    /// </summary>
    internal const string TransferKeyNetworkDownload = "TransferKeyNetworkDownload";

    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] protected readonly INetManager NetManager = default!;
    [Dependency] protected readonly IResourceManager ResourceManager = default!;
    [Dependency] protected readonly ITransferManager TransferManager = default!;
    [Dependency] protected readonly ILogManager LogManager = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;

    protected ISawmill Sawmill = default!;

    public const double BytesToMegabytes = 0.000001d;

    /// <summary>
    ///     The prefix for any and all downloaded network resources.
    /// </summary>
    private static readonly ResPath Prefix = ResPath.Root / "Uploaded";

    protected readonly MemoryContentRoot ContentRoot = new();

    public bool FileExists(ResPath path)
        => ContentRoot.FileExists(path);

    internal virtual void Initialize()
    {
        // Add our content root to the resource manager.
        ResourceManager.AddRoot(Prefix, ContentRoot);
        _replay.RecordingStarted += OnStartReplayRecording;
    }

    private void OnStartReplayRecording(MappingDataNode metadata, List<object> events)
    {
        // replays will need information about currently loaded extra resources
        foreach (var (path, data) in ContentRoot.GetAllFiles())
        {
            events.Add(new ReplayResourceUploadMsg { RelativePath = path, Data = data });
        }
    }

    protected internal void StoreFile(ResPath path, byte[] data)
    {
        ContentRoot.AddOrUpdateFile(path, data);
        _replay.RecordReplayMessage(new ReplayResourceUploadMsg { RelativePath = path, Data = data });
    }

    private async IAsyncEnumerable<(ResPath Relative, byte[] Data)> ReadTransferStream(Stream stream)
    {
        var lengthBytes = new byte[4];
        var continueByte = new byte[1];

        while (true)
        {
            await stream.ReadExactlyAsync(lengthBytes).ConfigureAwait(false);
            var pathLength = BinaryPrimitives.ReadUInt32LittleEndian(lengthBytes);

            await stream.ReadExactlyAsync(lengthBytes).ConfigureAwait(false);
            var dataLength = BinaryPrimitives.ReadUInt32LittleEndian(lengthBytes);

            ValidateUpload(dataLength);

            var pathData = new byte[pathLength];
            await stream.ReadExactlyAsync(pathData).ConfigureAwait(false);
            var data = new byte[dataLength];
            await stream.ReadExactlyAsync(data).ConfigureAwait(false);

            var path = new ResPath(Encoding.UTF8.GetString(pathData));
            yield return (path, data);

            await stream.ReadExactlyAsync(continueByte).ConfigureAwait(false);
            if (continueByte[0] == 0)
                break;
        }
    }

    protected virtual void ValidateUpload(uint size)
    {
    }

    protected async Task<List<(ResPath Relative, byte[] Data)>> IngestFileStream(Stream stream)
    {
        var list = new List<(ResPath Relative, byte[] Data)>();

        await foreach (var (relative, data) in ReadTransferStream(stream).ConfigureAwait(false))
        {
            Sawmill.Verbose($"Storing uploaded file: {relative} ({ByteHelpers.FormatBytes(data.Length)})");
            _taskManager.RunOnMainThread(() =>
            {
                StoreFile(relative, data);
            });
            list.Add((relative, data));
        }

        return list;
    }

    internal static async Task WriteFileStream(Stream stream, IEnumerable<(ResPath Relative, byte[] Data)> files)
    {
        var lengthBytes = new byte[4];
        var continueByte = new byte[1];

        var first = true;

        foreach (var (relative, data) in files)
        {
            if (!first)
            {
                continueByte[0] = 1;
                await stream.WriteAsync(continueByte).ConfigureAwait(false);
            }

            first = false;

            BinaryPrimitives.WriteUInt32LittleEndian(lengthBytes, (uint)Encoding.UTF8.GetByteCount(relative.CanonPath));
            await stream.WriteAsync(lengthBytes).ConfigureAwait(false);

            BinaryPrimitives.WriteUInt32LittleEndian(lengthBytes, (uint)data.Length);
            await stream.WriteAsync(lengthBytes).ConfigureAwait(false);

            await stream.WriteAsync(Encoding.UTF8.GetBytes(relative.CanonPath)).ConfigureAwait(false);
            await stream.WriteAsync(data).ConfigureAwait(false);
        }

        continueByte[0] = 0;
        await stream.WriteAsync(continueByte).ConfigureAwait(false);
    }

#pragma warning disable CA1816 // Not adding a finalizer...
    public void Dispose()
#pragma warning restore CA1816
    {
        // This is called automatically when the IoCManager's dependency collection is cleared.
        // MemoryContentRoot uses a ReaderWriterLockSlim, which we need to dispose of.
        ContentRoot.Dispose();
    }

    void IPostInjectInit.PostInject()
    {
        Sawmill = LogManager.GetSawmill("netres");
    }

    [Serializable, NetSerializable]
    internal sealed class ReplayResourceUploadMsg
    {
        public required byte[] Data;
        public required ResPath RelativePath;
    }
}
