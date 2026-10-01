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
using System.Threading.Tasks;
using Robust.Shared;
using Robust.Shared.Replays;
using static Robust.Shared.Replays.ReplayConstants;

namespace Robust.Client.Replays.Loading;

public sealed partial class ReplayLoadManager
{
    public async Task<ReplayData> LoadReplayAsync(IReplayFileReader fileReader, LoadReplayCallback callback)
    {
        // NOTE: fileReader is NOT disposed here. Ownership is transferred to the BufferedReplayDataProvider
        // below, which keeps reading data blocks lazily during playback and disposes the reader when the
        // replay is unloaded (ReplayPlaybackManager.StopReplay -> ReplayData.Dispose).

        if (_client.RunLevel == ClientRunLevel.Initialize)
            _client.StartSinglePlayer();
        else if (_client.RunLevel != ClientRunLevel.SinglePlayerGame)
            throw new Exception($"Invalid runlevel: {_client.RunLevel}.");

        _timing.Paused = true;

        var compressionContext = new ZStdCompressionContext();
        var metaData = LoadMetadata(fileReader);

        var totalData = fileReader.AllFiles.Count(x => x.Filename.StartsWith(DataFilePrefix));

        // Init messages are consumed at the very start of checkpoint generation, so load them up front.
        var initData = LoadInitFile(fileReader, compressionContext);
        compressionContext.Dispose();

        // Index of which data file backs which range of tick indices, so the provider can re-read blocks
        // lazily during playback instead of keeping everything resident.
        var blocks = new List<BufferedReplayDataProvider.BlockMeta>();
        var stats = new HistoryStreamStats { TotalBlocks = totalData };

        // The history is streamed block-by-block straight into checkpoint generation: at no point does the
        // whole deserialized replay sit in memory. Only the checkpoints (plus whatever per-entity states
        // they share with the last-seen history) survive the pass.
        var (checkpoints, serverTime) = await GenerateCheckpointsAsync(
            initData,
            metaData.CVars,
            StreamHistory(fileReader, totalData, blocks, stats),
            stats,
            callback);

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

    /// <summary>
    /// Aggregates facts about the replay history that only become known while streaming through it.
    /// Filled in by <see cref="StreamHistory"/> as the consumer advances the enumeration.
    /// </summary>
    private sealed class HistoryStreamStats
    {
        public GameTick FirstTick;
        public GameTick LastTick;
        public int TickCount;
        public int BlocksRead;
        public int TotalBlocks;
    }

    /// <summary>
    /// Lazily decodes the replay history one data block at a time, yielding (state, messages) pairs in tick
    /// order. Builds the <see cref="BufferedReplayDataProvider.BlockMeta"/> index as a side effect. Blocks
    /// become garbage as soon as the consumer moves past them, keeping the load-time memory peak flat.
    /// Loading-screen progress is reported solely by the consumer (checkpoint generation) so that the UI
    /// does not flip between the reading/processing phases every block.
    /// </summary>
    private IEnumerable<(GameState State, ReplayMessage Messages)> StreamHistory(
        IReplayFileReader fileReader,
        int totalData,
        List<BufferedReplayDataProvider.BlockMeta> blocks,
        HistoryStreamStats stats)
    {
        var i = 0;
        var intBuf = new byte[4];
        var name = new ResPath($"{DataFilePrefix}{i++}.{Ext}");
        while (fileReader.Exists(name))
        {
            var blockStart = stats.TickCount;
            var blockFile = name;

            using (var fileStream = fileReader.Open(name))
            using (var decompressStream = new ZStdDecompressStream(fileStream, false))
            {
                fileStream.ReadExactly(intBuf);
                var uncompressedSize = BitConverter.ToInt32(intBuf);

                var decompressedStream = new MemoryStream(uncompressedSize);
                decompressStream.CopyTo(decompressedStream);
                decompressedStream.Position = 0;
                DebugTools.Assert(uncompressedSize == decompressedStream.Length);

                while (decompressedStream.Position < decompressedStream.Length)
                {
                    _serializer.DeserializeDirect(decompressedStream, out GameState state);
                    _serializer.DeserializeDirect(decompressedStream, out ReplayMessage msg);

                    if (stats.TickCount == 0)
                        stats.FirstTick = state.ToSequence;
                    stats.LastTick = state.ToSequence;
                    stats.TickCount++;

                    yield return (state, msg);
                }
            }

            var blockCount = stats.TickCount - blockStart;
            if (blockCount > 0)
                blocks.Add(new BufferedReplayDataProvider.BlockMeta(blockFile, blockStart, blockCount));
            stats.BlocksRead++;

            name = new ResPath($"{DataFilePrefix}{i++}.{Ext}");
        }

        // Could happen if there's gaps in the numbers of the data.
        if (i - 1 != totalData)
            throw new Exception("Could not read expected amount of data files from replay");
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
