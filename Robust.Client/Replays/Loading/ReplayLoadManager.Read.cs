using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Threading.Channels;
using Robust.Shared;
using Robust.Shared.Replays;
using static Robust.Shared.Replays.ReplayConstants;

namespace Robust.Client.Replays.Loading;

public sealed partial class ReplayLoadManager
{
    public async Task<ReplayData> LoadReplayAsync(IReplayFileReader fileReader, LoadReplayJob? job, LoadReplayCallback callback)
    {
        // NOTE: fileReader is NOT disposed here. Ownership is transferred to the BufferedReplayDataProvider
        // below, which keeps reading data blocks lazily during playback and disposes the reader when the
        // replay is unloaded (ReplayPlaybackManager.StopReplay -> ReplayData.Dispose).

        if (_client.RunLevel == ClientRunLevel.Initialize)
            _client.StartSinglePlayer();
        else if (_client.RunLevel != ClientRunLevel.SinglePlayerGame)
            throw new Exception($"Invalid runlevel: {_client.RunLevel}.");

        _timing.Paused = true;

        var metaData = LoadMetadata(fileReader);

        var totalData = fileReader.AllFiles.Count(x => x.Filename.StartsWith(DataFilePrefix));

        // Init messages are consumed at the very start of checkpoint generation, so load them up front.
        var initData = LoadInitFile(fileReader);

        // Index of which data file backs which range of tick indices, so the provider can re-read blocks
        // lazily during playback instead of keeping everything resident.
        var blocks = new List<BufferedReplayDataProvider.BlockMeta>();
        var stats = new HistoryStreamStats();

        // Keep both background stages bounded so loading never retains the entire replay history.
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var checkpointChannel = Channel.CreateBounded<ReplayTickData>(Math.Max(1, _checkpointInterval));
        var decompressedFiles = Channel.CreateBounded<(ResPath File, MemoryStream Stream)>(2);
        var reusableStreams = Channel.CreateBounded<MemoryStream>(new BoundedChannelOptions(4)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        });

        var decompressTask = Task.Run(async () =>
        {
            Exception? error = null;
            try
            {
                var i = 0;
                var intBuf = new byte[4];
                var name = new ResPath($"{DataFilePrefix}{i++}.{Ext}");
                while (fileReader.Exists(name))
                {
                    token.ThrowIfCancellationRequested();
                    using var fileStream = fileReader.Open(name);
                    fileStream.ReadExactly(intBuf);
                    var uncompressedSize = BitConverter.ToInt32(intBuf);
                    using var decompressStream = new ZStdDecompressStream(fileStream, false);
                    if (!reusableStreams.Reader.TryRead(out var stream))
                        stream = new MemoryStream(uncompressedSize);
                    stream.SetLength(0);
                    decompressStream.CopyTo(stream);
                    DebugTools.Assert(uncompressedSize == stream.Length);
                    stream.Position = 0;
                    await decompressedFiles.Writer.WriteAsync((name, stream), token);
                    name = new ResPath($"{DataFilePrefix}{i++}.{Ext}");
                }

                if (i - 1 != totalData)
                    throw new Exception("Could not read expected amount of data files from replay");
            }
            catch (Exception e)
            {
                error = e;
                throw;
            }
            finally
            {
                decompressedFiles.Writer.TryComplete(error);
            }
        });

        var deserializeTask = Task.Run(async () =>
        {
            Exception? error = null;
            try
            {
                var progress = 0;
                await foreach (var (file, stream) in decompressedFiles.Reader.ReadAllAsync(token))
                {
                    var blockStart = stats.TickCount;
                    while (stream.Position < stream.Length)
                    {
                        token.ThrowIfCancellationRequested();
                        _serializer.DeserializeDirect(stream, out GameState state);
                        _serializer.DeserializeDirect(stream, out ReplayMessage msg);
                        if (stats.TickCount == 0)
                            stats.FirstTick = state.ToSequence;
                        stats.LastTick = state.ToSequence;
                        var stateId = stats.TickCount++;
                        await checkpointChannel.Writer.WriteAsync(
                            new ReplayTickData(stateId, state, msg, progress + 1, totalData), token);
                    }

                    var blockCount = stats.TickCount - blockStart;
                    if (blockCount > 0)
                        blocks.Add(new BufferedReplayDataProvider.BlockMeta(file, blockStart, blockCount));
                    reusableStreams.Writer.TryWrite(stream);
                    progress++;
                }
            }
            catch (Exception e)
            {
                error = e;
                throw;
            }
            finally
            {
                checkpointChannel.Writer.TryComplete(error);
            }
        });

        CheckpointState[] checkpoints;
        TimeSpan[] serverTime;
        Exception? checkpointError = null;
        try
        {
            (checkpoints, serverTime) = await GenerateCheckpointsAsync(
                initData, metaData.CVars, checkpointChannel.Reader, totalData, callback, job);
        }
        catch (Exception e)
        {
            checkpointError = e;
            throw;
        }
        finally
        {
            // Unblock either producer if checkpoint generation fails while a channel is full.
            cancellation.Cancel();
            var producers = Task.WhenAll(decompressTask, deserializeTask);
            try
            {
                if (job != null)
                    await job.WaitAsyncTask(producers);
                else
                    await producers;
            }
            catch when (checkpointError != null)
            {
                // Preserve the checkpoint failure rather than replacing it with producer cancellation.
            }
        }

        _timing.Paused = false;

        if (stats.TickCount == 0)
            throw new Exception("Replay contains no game states");

        var provider = new BufferedReplayDataProvider(
            fileReader,
            _serializer,
            blocks.ToArray(),
            stats.TickCount,
            _loadedBlockWindow,
            _sawmill);

        // The streaming pass churns a lot of transient per-block data, some of which gets promoted to
        // Gen2/LOH before dying. Compact once so playback starts with a tight heap. One-off cost.
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

        _sawmill.Info($"[BUFFER] Streamed load done. Managed heap now {GC.GetTotalMemory(false) / 1024.0 / 1024.0:N0} MB " +
                      $"({blocks.Count} data blocks indexed, window={_loadedBlockWindow}, checkpoints={checkpoints.Length}). " +
                      $"Remaining growth during playback is the live entity world, not replay history.");

        return new ReplayData(
            provider,
            serverTime,
            stats.FirstTick,
            stats.LastTick,
            metaData.StartTime,
            metaData.Duration,
            checkpoints,
            initData,
            metaData.ClientSide,
            metaData.YamlData);
    }

    // Written by the deserialization worker and consumed after both workers have completed.
    private sealed class HistoryStreamStats
    {
        public GameTick FirstTick;
        public GameTick LastTick;
        public int TickCount;
    }

    private ReplayMessage? LoadInitFile(
        IReplayFileReader fileReader)
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
        _serializer.SetStringSerializerPackage(stringHash, stringFile.CopyToArray());

        using var cvarsFile = fileReader.Open(FileCvars);
        // Note, this does not invoke the received-initial-cvars event. But at least currently, that doesn't matter
        var cvars = _confMan.LoadFromTomlStream(cvarsFile);

        _timing.CurTick = new GameTick(uint.Parse(startTick));
        _timing.TimeBase = (new TimeSpan(long.Parse(timeBaseTimespan)), new GameTick(uint.Parse(timeBaseTick)));

        _sawmill.Info($"Successfully read metadata");
        return (data, cvars, duration, _timing.CurTime, clientSide);
    }
}
