using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Robust.Shared;
using Robust.Shared.IoC;
using Robust.Shared.Replays;
using static Robust.Shared.Replays.ReplayConstants;

namespace Robust.Client.Replays.Loading;

public sealed partial class ReplayLoadManager
{
    private struct DecompressedFile
    {
        // Store the filesize seperately so we can use streams that are "too large" allowing reuse of MemoryStreams between files
        public int FileSize;
        public MemoryStream Stream;

        public DecompressedFile(MemoryStream stream, int fileSize)
        {
            this.Stream = stream;
            this.FileSize = fileSize;
        }
    }

    [SuppressMessage("ReSharper", "UseAwaitUsing")]
    public async Task<ReplayData> LoadReplayAsync(IReplayFileReader fileReader, LoadReplayCallback callback)
    {
        using var _ = fileReader;

        if (_client.RunLevel == ClientRunLevel.Initialize)
            _client.StartSinglePlayer();
        else if (_client.RunLevel != ClientRunLevel.SinglePlayerGame)
            throw new Exception($"Invalid runlevel: {_client.RunLevel}.");

        // Ensure we are showing no progress while we load state0 (after that checkpoint code updates progress)
        await callback(0, 1, LoadingState.ReadingFiles, false);

        _timing.Paused = true;
        List<GameState> states = new();
        List<ReplayMessage> messages = new();

        var compressionContext = new ZStdCompressionContext();
        var metaData = LoadMetadata(fileReader);

        var totalData = fileReader.AllFiles.Count(x => x.Filename.StartsWith(DataFilePrefix));

        MemoryStream? decompressedStream = null;

        // Only allow the file reader to get _checkpointInterval ticks ahead of the checkpoint creation thread.
        // This improves memory locality because it means the checkpoint logic is always reading recently written data.
        // If the reader gets too far ahead, data relevant to the checkpoint system will get flushed out of cache.
        var checkpointChannel = Channel.CreateBounded<ReplayTickData>(_checkpointInterval);

        // Files are opened and pre-processed in the background, using these streams
        // Decompressed files in a memory stream ready for reading:
        var decompressedFileChannel = Channel.CreateBounded<DecompressedFile>(2);
        // Used memory channels ready to be reused without reallocating them. If there are several already, we just
        //  drop the one being written - no point having too many.
        var reuseFileChannel = Channel.CreateBounded<MemoryStream>(
            new BoundedChannelOptions(4)
            {
                FullMode = BoundedChannelFullMode.DropWrite
            });

        var initData = LoadInitFile(fileReader, compressionContext);
        // Capture this thread's iocInstance for our subthread
        var iocInstance = IoCManager.Instance!;

        // The Decompression task reads files from the replay and creates decompressed MemoryStreams for the next task
        // Old memory streams are passed back here for reuse.
        var decompressTask = Task.Run(async () =>
        {
            var i = 0;
            var intBuf = new byte[4];
            var name = new ResPath($"{DataFilePrefix}{i++}.{Ext}");

            while (fileReader.Exists(name))
            {
                using var fileStream = fileReader.Open(name);
                using var decompressStream = new ZStdDecompressStream(fileStream, false);

                fileStream.ReadExactly(intBuf);
                var uncompressedSize = BitConverter.ToInt32(intBuf);

                MemoryStream? decompressedStream = null;
                reuseFileChannel.Reader.TryRead(out decompressedStream);
                if (decompressedStream == null || decompressedStream.Length < uncompressedSize)
                {
                    // Double required size to increase chance of reuse by next slightly larger file.
                    decompressedStream = new MemoryStream(uncompressedSize * 2);
                }

                // Set position to 0 ready to decompress data into it
                decompressedStream.Position = 0;
                decompressStream.CopyTo(decompressedStream, uncompressedSize);

                // Prepare for a read from the start of it
                decompressedStream.Position = 0;
                await decompressedFileChannel.Writer.WriteAsync(new DecompressedFile(decompressedStream,
                    uncompressedSize));

                name = new ResPath($"{DataFilePrefix}{i++}.{Ext}");
            }
            // Could happen if there's gaps in the numbers of the data.
            if (i - 1 != totalData)
                throw new Exception("Could not read expected amount of data files from replay");

            decompressedFileChannel.Writer.Complete();
        });

        // The deserializeTask processes decompressed file streams from decompressTask and deserializes their contents
        // into replay ticks consumed by GenerateCheckpointsAsync
        // Once a memory stream is fully read, we send it back to decompressTask to reuse the allocation.
        var deserializeTask = Task.Run(async () =>
        {
            // Keep a local file counter to track progress
            int i = 0;
            await foreach (var decompressed in decompressedFileChannel.Reader.ReadAllAsync())
            {
                DebugTools.Assert(decompressed.FileSize <= decompressed.Stream.Length);

                while (decompressed.Stream.Position < decompressed.FileSize)
                {
                    _serializer.DeserializeDirect(decompressed.Stream, out GameState state);
                    _serializer.DeserializeDirect(decompressed.Stream, out ReplayMessage msg);
                    states.Add(state);
                    messages.Add(msg);
                    // Send this tick to be processed for checkpoints (this might block until checkpoint creation catches up)
                    await checkpointChannel.Writer.WriteAsync(new ReplayTickData(states.Count - 1, state, msg, i, totalData));
                }

                // Send the memory stream back for reuse
                await reuseFileChannel.Writer.WriteAsync(decompressed.Stream);
                i += 1;

                if (i % 10 == 1)
                {
                    _sawmill.Info($"Deserialized {i}. Decompressed files {decompressedFileChannel.Reader.Count}. Queued states {checkpointChannel.Reader.Count}. Total ticks {states.Count}");
                }
            }

            // Done creating replay data to process in the background
            checkpointChannel.Writer.Complete();
            compressionContext.Dispose();
        });

        // Wait for checkpoints to complete
        var (checkpoints, serverTime) = await GenerateCheckpointsAsync(
                initData,
                metaData.CVars,
                checkpointChannel.Reader,
                callback);

        // Ensure book-keeping for our background tasks has a chance to complete.
        //  Note - decompressTask and deserializeTask need to be awaited AFTER GenerateCheckpointsAsync because they
        //         will internally pause if GenerateCheckpointsAsync is not keeping up.
        await decompressTask;
        await deserializeTask;

        _timing.Paused = false;
        return new ReplayData(
            states,
            messages,
            serverTime,
            states[0].ToSequence,
            metaData.StartTime,
            metaData.Duration,
            checkpoints,
            initData,
            metaData.ClientSide,
            metaData.YamlData);
    }

    private ReplayMessage? LoadInitFile(
        IReplayFileReader fileReader,
        ZStdCompressionContext compressionContext)
    {
        if (!fileReader.Exists(FileInit))
            return null;

        // TODO replays compress init messages, then decompress them here.
        using var fileStream = fileReader.Open(FileInit);
        _serializer.DeserializeDirect(fileStream, out ReplayMessage initData);
        return initData;
    }

    public MappingDataNode? LoadYamlMetadata(IReplayFileReader fileReader)
    {
        return LoadYamlFile(fileReader, FileMeta);
    }

    public MappingDataNode? LoadYamlFinalMetadata(IReplayFileReader fileReader)
    {
        return LoadYamlFile(fileReader, FileMetaFinal);
    }

    private static MappingDataNode? LoadYamlFile(IReplayFileReader fileReader, ResPath path)
    {
        if (!fileReader.Exists(path))
            return null;

        using var file = fileReader.Open(path);
        var parsed = DataNodeParser.ParseYamlStream(new StreamReader(file));
        return parsed.FirstOrDefault()?.Root as MappingDataNode;
    }

    private (MappingDataNode YamlData, HashSet<string> CVars, TimeSpan? Duration, TimeSpan StartTime, bool ClientSide)
        LoadMetadata(IReplayFileReader fileReader)
    {
        _sawmill.Info($"Reading replay metadata");
        var data = LoadYamlMetadata(fileReader);
        if (data == null)
            throw new Exception("Failed to load yaml metadata");

        var finalData = LoadYamlFinalMetadata(fileReader);
        TimeSpan? duration = finalData == null
            ? null
            : TimeSpan.Parse(((ValueDataNode) finalData[MetaFinalKeyDuration]).Value);

        if (finalData == null)
            _sawmill.Warning("Failed to load final yaml metadata. Partial/incomplete replay?");

        var typeHashString = ((ValueDataNode) data[MetaKeyTypeHash]).Value;
        var typeHash = Convert.FromHexString(typeHashString);
        var stringHash = Convert.FromHexString(((ValueDataNode) data[MetaKeyStringHash]).Value);
        var startTick = ((ValueDataNode) data[MetaKeyStartTick]).Value;
        var timeBaseTick = ((ValueDataNode) data[MetaKeyBaseTick]).Value;
        var timeBaseTimespan = ((ValueDataNode) data[MetaKeyBaseTime]).Value;
        var clientSide = bool.Parse(((ValueDataNode) data[MetaKeyIsClientRecording]).Value);

        if (!typeHash.SequenceEqual(_serializer.GetSerializableTypesHash()))
        {
            if (!_confMan.GetCVar(CVars.ReplayIgnoreErrors))
                throw new Exception($"RobustSerializer hash mismatch. do not match. Client hash: {_serializer.GetSerializableTypesHashString()}, replay hash: {typeHashString}.");

            _sawmill.Warning($"RobustSerializer hash mismatch. Replay may fail to load!");
        }

        using var stringFile = fileReader.Open(FileStrings);
        var stringData = new byte[stringFile.Length];
        stringFile.ReadExactly(stringData);
        _serializer.SetStringSerializerPackage(stringHash, stringData);

        using var cvarsFile = fileReader.Open(FileCvars);
        // Note, this does not invoke the received-initial-cvars event. But at least currently, that doesn't matter
        var cvars = _confMan.LoadFromTomlStream(cvarsFile);

        _timing.CurTick = new GameTick(uint.Parse(startTick));
        _timing.TimeBase = (new TimeSpan(long.Parse(timeBaseTimespan)), new GameTick(uint.Parse(timeBaseTick)));

        _sawmill.Info($"Successfully read metadata");
        return (data, cvars, duration, _timing.CurTime, clientSide);
    }
}
