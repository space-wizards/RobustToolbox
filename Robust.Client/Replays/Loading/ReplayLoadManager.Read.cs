using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.ContentPack;
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
using Robust.Shared.Replays;
using static Robust.Shared.Replays.IReplayRecordingManager;

namespace Robust.Client.Replays.Loading;

public sealed partial class ReplayLoadManager
{
    [SuppressMessage("ReSharper", "UseAwaitUsing")]
    public async Task<ReplayData> LoadReplayAsync(IWritableDirProvider dir, LoadReplayCallback callback)
    {
        List<GameState> states = new();
        List<ReplayMessage> messages = new();

        var compressionContext = new ZStdCompressionContext();
        var metaData = LoadMetadata(dir);

        var total = dir.Find($"*.{Ext}").files.Count();

        // Exclude string & init event files from the total.
        total--;
        if (dir.Exists(InitFile))
            total--;

        var i = 0;
        var intBuf = new byte[4];
        var name = new ResPath($"/{i++}.{Ext}");
        while (dir.Exists(name))
        {
            await callback(i+1, total, LoadingState.ReadingFiles, false);

            using var fileStream = dir.OpenRead(name);
            using var decompressStream = new ZStdDecompressStream(fileStream, false);

            fileStream.Read(intBuf);
            var uncompressedSize = BitConverter.ToInt32(intBuf);

            var decompressedStream = new MemoryStream(uncompressedSize);
            decompressStream.CopyTo(decompressedStream, uncompressedSize);
            decompressedStream.Position = 0;

            while (decompressedStream.Position < decompressedStream.Length)
            {
                _serializer.DeserializeDirect(decompressedStream, out GameState state);
                _serializer.DeserializeDirect(decompressedStream, out ReplayMessage msg);
                states.Add(state);
                messages.Add(msg);
            }

            name = new ResPath($"/{i++}.{Ext}");
        }
        DebugTools.Assert(i - 1 == total);
        await callback(total, total, LoadingState.ReadingFiles, false);

        var initData = LoadInitFile(dir, compressionContext);
        compressionContext.Dispose();

        var checkpoints = await GenerateCheckpointsAsync(initData, metaData.CVars, states, messages, callback);
        return new(states, messages, states[0].ToSequence, metaData.StartTime, metaData.Duration, checkpoints, initData);
    }

    private ReplayMessage? LoadInitFile(IWritableDirProvider dir, ZStdCompressionContext compressionContext)
    {
        if (!dir.Exists(InitFile))
            return null;

        // TODO compress init messages, then decompress them here.
        using var fileStream = dir.OpenRead(InitFile);
        _serializer.DeserializeDirect(fileStream, out ReplayMessage initData);
        return initData;
    }

    public MappingDataNode? LoadYamlMetadata(IWritableDirProvider directory)
    {
        if (!directory.Exists(MetaFile))
            return null;

        using var file = directory.OpenRead(MetaFile);
        var parsed = DataNodeParser.ParseYamlStream(new StreamReader(file));
        return parsed.FirstOrDefault()?.Root as MappingDataNode;
    }

    public (HashSet<string> CVars, TimeSpan Duration, TimeSpan StartTime) LoadMetadata(IWritableDirProvider directory)
    {
        _sawmill.Info($"Reading replay metadata");
        var data = LoadYamlMetadata(directory);
        if (data == null)
            throw new Exception("Failed to parse yaml metadata");

        var typeHash = Convert.FromHexString(((ValueDataNode) data[Hash]).Value);
        var stringHash = Convert.FromHexString(((ValueDataNode) data[Strings]).Value);
        var startTick = ((ValueDataNode) data[Tick]).Value;
        var timeBaseTick = ((ValueDataNode) data[BaseTick]).Value;
        var timeBaseTimespan = ((ValueDataNode) data[BaseTime]).Value;
        var duration = TimeSpan.Parse(((ValueDataNode) data[Duration]).Value);

        if (!typeHash.SequenceEqual(_serializer.GetSerializableTypesHash()))
            throw new Exception($"{nameof(IRobustSerializer)} hashes do not match. Loading replays using a bad replay-client version?");

        using var stringFile = directory.OpenRead(StringsFile);
        var stringData = new byte[stringFile.Length];
        stringFile.Read(stringData);
        _serializer.SetStringSerializerPackage(stringHash, stringData);

        using var cvarsFile = directory.OpenRead(CvarFile);
        // Note, this does not invoke the received-initial-cvars event. But at least currently, that doesn't matter
        var cvars = _confMan.LoadFromTomlStream(cvarsFile);

        _timing.CurTick = new GameTick(uint.Parse(startTick));
        _timing.TimeBase = (new TimeSpan(long.Parse(timeBaseTimespan)), new GameTick(uint.Parse(timeBaseTick)));

        _sawmill.Info($"Successfully read metadata");
        return (cvars, duration, _timing.CurTime);
    }
}
