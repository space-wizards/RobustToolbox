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
    [SuppressMessage("ReSharper", "UseAwaitUsing")]
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
        List<GameState> states = new();
        List<ReplayMessage> messages = new();

        var compressionContext = new ZStdCompressionContext();
        var metaData = LoadMetadata(fileReader);

        var totalData = fileReader.AllFiles.Count(x => x.Filename.StartsWith(DataFilePrefix));

        // Index of which data file backs which range of tick indices, so the provider can re-read blocks
        // lazily during playback instead of keeping everything resident.
        var blocks = new List<BufferedReplayDataProvider.BlockMeta>();

        var i = 0;
        var intBuf = new byte[4];
        var name = new ResPath($"{DataFilePrefix}{i++}.{Ext}");
        while (fileReader.Exists(name))
        {
            await callback(i+1, totalData, LoadingState.ReadingFiles, false);

            var blockStart = states.Count;
            var blockFile = name;

            using var fileStream = fileReader.Open(name);
            using var decompressStream = new ZStdDecompressStream(fileStream, false);

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
                states.Add(state);
                messages.Add(msg);
            }

            var blockCount = states.Count - blockStart;
            if (blockCount > 0)
                blocks.Add(new BufferedReplayDataProvider.BlockMeta(blockFile, blockStart, blockCount));

            name = new ResPath($"{DataFilePrefix}{i++}.{Ext}");
        }

        // Could happen if there's gaps in the numbers of the data.
        if (i - 1 != totalData)
            throw new Exception("Could not read expected amount of data files from replay");

        await callback(totalData, totalData, LoadingState.ReadingFiles, false);

        var initData = LoadInitFile(fileReader, compressionContext);
        compressionContext.Dispose();

        var (checkpoints, serverTime) = await GenerateCheckpointsAsync(
            initData,
            metaData.CVars,
            states, messages,
            callback);

        _timing.Paused = false;

        // Capture what we need before dropping the in-memory lists; from here on states/messages are
        // served lazily by the provider, which re-reads blocks from the (still-open) replay file and
        // owns its lifetime.
        var tickOffset = states[0].ToSequence;
        var lastTick = states[^1].ToSequence;
        var tickCount = states.Count;
        var provider = new BufferedReplayDataProvider(
            fileReader,
            _serializer,
            blocks.ToArray(),
            tickCount,
            _loadedBlockWindow,
            _sawmill);

        // Drop the full history. NOTE: nulling the local references is not enough — the async load/checkpoint
        // call chain keeps other references to these list objects alive, so the elements would stay reachable.
        // Clear() empties the lists from the inside (releasing every GameState/ReplayMessage) regardless of who
        // else still holds the container. These objects survived the load into Gen2/LOH, so we then force a
        // compacting collection to actually return the memory to the OS before playback begins. One-off cost.
        states.Clear();
        states.TrimExcess();
        messages.Clear();
        messages.TrimExcess();
        states = null!;
        messages = null!;
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

        _sawmill.Info($"[BUFFER] History dropped. Managed heap now {GC.GetTotalMemory(false) / 1024.0 / 1024.0:N0} MB " +
                      $"({blocks.Count} data blocks indexed, window={_loadedBlockWindow}, checkpoints={checkpoints.Length}). " +
                      $"Remaining growth during playback is the live entity world, not replay history.");

        return new ReplayData(
            provider,
            serverTime,
            tickOffset,
            lastTick,
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
