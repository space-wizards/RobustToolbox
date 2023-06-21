using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SharpZstd.Interop;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Robust.Shared.Network;
using YamlDotNet.RepresentationModel;
using static Robust.Shared.Replays.IReplayRecordingManager;

namespace Robust.Shared.Replays;

internal abstract partial class SharedReplayRecordingManager : IReplayRecordingManager
{
    // date format for default replay names. Like the sortable template, but without colons.
    public const string DefaultReplayNameFormat = "yyyy-MM-dd_HH-mm-ss";

    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly INetConfigurationManager NetConf = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IRobustSerializer _serializer = default!;
    [Dependency] private readonly INetManager _netMan = default!;

    public event Action<MappingDataNode, List<object>>? RecordingStarted;
    public event Action<MappingDataNode>? RecordingStopped;
    public event Action<IWritableDirProvider, ResPath>? RecordingFinished;


    private ISawmill _sawmill = default!;
    private List<object> _queuedMessages = new();

    private int _maxCompressedSize;
    private int _maxUncompressedSize;
    private int _tickBatchSize;
    private bool _enabled;

    public bool IsRecording => _replay != null;
    private (MemoryStream Stream, ZStdCompressionContext Context)? _replay;

    private int _index = 0;
    private int _currentCompressedSize;
    private int _currentUncompressedSize;
    private (GameTick Tick, TimeSpan Time) _recordingStart;
    private TimeSpan? _recordingEnd;
    private MappingDataNode? _yamlMetadata;
    private (IWritableDirProvider, ResPath)? _directory;

    /// <inheritdoc/>
    public virtual void Initialize()
    {
        _sawmill = Logger.GetSawmill("replay");
        NetConf.OnValueChanged(CVars.ReplayMaxCompressedSize, (v) => _maxCompressedSize = v * 1024, true);
        NetConf.OnValueChanged(CVars.ReplayMaxUncompressedSize, (v) => _maxUncompressedSize = v * 1024, true);
        NetConf.OnValueChanged(CVars.ReplayTickBatchSize, (v) => _tickBatchSize = v * 1024, true);
        NetConf.OnValueChanged(CVars.NetPVSCompressLevel, OnCompressionChanged);
    }

    public virtual bool CanStartRecording()
    {
        return !IsRecording && _enabled;
    }

    private void OnCompressionChanged(int value)
    {
        if (_replay is var (_, context))
            context.SetParameter(ZSTD_cParameter.ZSTD_c_compressionLevel, value);
    }

    public void SetReplayEnabled(bool value)
    {
        if (!value)
            StopRecording();

        _enabled = value;
    }

    /// <inheritdoc/>
    public void StopRecording()
    {
        if (_replay == null)
            return;

        try
        {
            WriteGameState(continueRecording: false);
            _sawmill.Info("Replay recording stopped!");
        }
        catch
        {
            Reset();
            throw;
        }

        UpdateWriteTasks();
    }

    public void Update(GameState? state)
    {
        UpdateWriteTasks();

        if (state == null || _replay is not var (stream, _))
            return;

        try
        {
            _serializer.SerializeDirect(stream, state);
            _serializer.SerializeDirect(stream, new ReplayMessage { Messages = _queuedMessages });
            _queuedMessages.Clear();

            bool continueRecording = _recordingEnd == null || _recordingEnd.Value >= Timing.CurTime;
            if (!continueRecording)
                _sawmill.Info("Reached requested replay recording length. Stopping recording.");

            if (!continueRecording || stream.Length > _tickBatchSize)
                WriteGameState(continueRecording);
        }
        catch (Exception e)
        {
            _sawmill.Log(LogLevel.Error, e, "Caught exception while saving replay data.");
            StopRecording();
        }
    }

    /// <inheritdoc/>
    public virtual bool TryStartRecording(
        IWritableDirProvider directory,
        string? name = null,
        bool overwrite = false,
        TimeSpan? duration = null)
    {
        if (!CanStartRecording())
            return false;

        // If the previous recording had exceptions, throw them now before starting a new recording.
        UpdateWriteTasks();

        ResPath subDir;
        if (name == null)
        {
            name = DateTime.UtcNow.ToString(DefaultReplayNameFormat);
            subDir = new ResPath(name);
        }
        else
        {
            subDir = new ResPath(name).Clean();
            if (subDir == ResPath.Root || subDir == ResPath.Empty || subDir == ResPath.Self)
                subDir = new ResPath(DateTime.UtcNow.ToString(DefaultReplayNameFormat));
        }

        var basePath = new ResPath(NetConf.GetCVar(CVars.ReplayDirectory)).ToRootedPath();
        subDir = basePath / subDir.ToRelativePath();

        if (directory.Exists(subDir))
        {
            if (overwrite)
            {
                _sawmill.Info($"Replay folder {subDir} already exists. Overwriting.");
                directory.Delete(subDir);
            }
            else
            {
                _sawmill.Info($"Replay folder {subDir} already exists. Aborting recording.");
                return false;
            }
        }
        directory.CreateDir(subDir);
        _directory = (directory, subDir);

        var context = new ZStdCompressionContext();
        context.SetParameter(ZSTD_cParameter.ZSTD_c_compressionLevel, NetConf.GetCVar(CVars.NetPVSCompressLevel));
        _replay = (new MemoryStream(_tickBatchSize * 2), context);
        _index = 0;
        _recordingStart = (Timing.CurTick, Timing.CurTime);

        try
        {
            WriteInitialMetadata(name);
        }
        catch
        {
            Reset();
            throw;
        }

        if (duration != null)
            _recordingEnd = Timing.CurTime + duration.Value;

        _sawmill.Info("Started recording replay...");
        UpdateWriteTasks();
        return true;
    }

    public abstract void RecordServerMessage(object obj);
    public abstract void RecordClientMessage(object obj);

    public void RecordReplayMessage(object obj)
    {
        if (!IsRecording)
            return;

        DebugTools.Assert(obj.GetType().HasCustomAttribute<NetSerializableAttribute>());
        _queuedMessages.Add(obj);
    }

    private void WriteGameState(bool continueRecording = true)
    {
        if (_replay is not var (stream, context) || _directory is not var (dir, path))
            return;

        stream.Position = 0;

        // Compress stream to buffer.
        // First 4 bytes of buffer are reserved for the length of the uncompressed stream.
        var bound = ZStd.CompressBound((int) stream.Length);
        var buf = ArrayPool<byte>.Shared.Rent(4 + bound);
        var length = context.Compress2( buf.AsSpan(4, bound), stream.AsSpan());
        BitConverter.TryWriteBytes(buf, (int)stream.Length);
        WritePooledBytes(buf, 4 + length, dir, path / $"{_index++}.{Ext}");

        _currentUncompressedSize += (int)stream.Length;
        _currentCompressedSize += length;
        if (_currentUncompressedSize >= _maxUncompressedSize || _currentCompressedSize >= _maxCompressedSize)
        {
            _sawmill.Info("Reached max replay recording size. Stopping recording.");
            continueRecording = false;
        }

        if (continueRecording)
            stream.SetLength(0);
        else
            WriteFinalMetadata();
    }

    protected virtual void Reset()
    {
        if (_replay is var (stream, context))
        {
            stream.Dispose();
            context.Dispose();
        }

        _replay = null;
        _currentCompressedSize = 0;
        _currentUncompressedSize = 0;
        _index = 0;
        _recordingEnd = null;
        _directory = null;
    }

    /// <summary>
    ///     Write general replay data required to read the rest of the replay. We write this at the beginning rather than at the end on the off-chance that something goes wrong along the way and the recording is incomplete.
    /// </summary>
    private void WriteInitialMetadata(string name)
    {
        if (_directory is not var (dir, path))
            return;

        var (stringHash, stringData) = _serializer.GetStringSerializerPackage();
        var extraData = new List<object>();

        // Saving YAML data. This gets overwritten later anyways, this is mostly in case something goes wrong.
        {
            _yamlMetadata = new MappingDataNode();
            _yamlMetadata[Time] = new ValueDataNode(DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
            _yamlMetadata[Name] = new ValueDataNode(name);

            // version info
            _yamlMetadata[Engine] = new ValueDataNode(NetConf.GetCVar(CVars.BuildEngineVersion));
            _yamlMetadata[Fork] = new ValueDataNode(NetConf.GetCVar(CVars.BuildForkId));
            _yamlMetadata[ForkVersion] = new ValueDataNode(NetConf.GetCVar(CVars.BuildVersion));

            // Hash data
            _yamlMetadata[Hash] = new ValueDataNode(Convert.ToHexString(_serializer.GetSerializableTypesHash()));
            _yamlMetadata[Strings] = new ValueDataNode(Convert.ToHexString(stringHash));
            _yamlMetadata[CompHash] = new ValueDataNode(Convert.ToHexString(_factory.GetHash(true)));

            // Time data
            var timeBase = Timing.TimeBase;
            _yamlMetadata[Tick] = new ValueDataNode(_recordingStart.Tick.Value.ToString());
            _yamlMetadata[BaseTick] = new ValueDataNode(timeBase.Item2.Value.ToString());
            _yamlMetadata[BaseTime] = new ValueDataNode(timeBase.Item1.Ticks.ToString());
            _yamlMetadata[ServerTime] = new ValueDataNode(_recordingStart.Time.ToString());

            _yamlMetadata[IsClient] = new ValueDataNode(_netMan.IsClient.ToString());

            RecordingStarted?.Invoke(_yamlMetadata, extraData);

            var document = new YamlDocument(_yamlMetadata.ToYaml());
            WriteYaml(document, dir, path / MetaFile);
        }

        // Saving misc extra data like networked messages that typically get sent to newly connecting clients.
        // TODO REPLAYS compression
        // currently resource uploads are uncompressed, so this might be quite big.
        if (extraData.Count > 0)
            WriteSerializer(new ReplayMessage { Messages = extraData }, dir, path / InitFile);

        // save data required for IRobustMappedStringSerializer
        WriteBytes(stringData, dir, path / StringsFile);

        // Save replicated cvars.
        var cvars = NetConf.GetReplicatedVars(true).Select(x => x.name);
        WriteToml(cvars, dir, path / CvarFile );
    }

    private void WriteFinalMetadata()
    {
        if (_yamlMetadata == null || _directory is not var (dir, path))
            return;

        RecordingStopped?.Invoke(_yamlMetadata);
        var time = Timing.CurTime - _recordingStart.Time;
        _yamlMetadata[EndTick] = new ValueDataNode(Timing.CurTick.Value.ToString());
        _yamlMetadata[Duration] = new ValueDataNode(time.ToString());
        _yamlMetadata[FileCount] = new ValueDataNode(_index.ToString());
        _yamlMetadata[Compressed] = new ValueDataNode(_currentCompressedSize.ToString());
        _yamlMetadata[Uncompressed] = new ValueDataNode(_currentUncompressedSize.ToString());
        _yamlMetadata[EndTime] = new ValueDataNode(Timing.CurTime.ToString());

        // this just overwrites the previous yml with additional data.
        var document = new YamlDocument(_yamlMetadata.ToYaml());
        WriteYaml(document, dir, path / MetaFile);
        UpdateWriteTasks();
        RecordingFinished?.Invoke(dir, path);
        Reset();
    }

    public (float Minutes, int Ticks, float Size, float UncompressedSize) GetReplayStats()
    {
        if (!IsRecording)
            return default;

        var time = (Timing.CurTime - _recordingStart.Time).TotalMinutes;
        var tick = Timing.CurTick.Value - _recordingStart.Tick.Value;
        var size = _currentCompressedSize / (1024f * 1024f);
        var altSize = _currentUncompressedSize / (1024f * 1024f);

        return ((float)time, (int)tick, size, altSize);
    }
}
