using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Replays;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SharpZstd.Interop;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using static Robust.Server.GameStates.ServerGameStateManager;

namespace Robust.Server.Replays;

internal sealed class ReplayRecordingManager : IInternalReplayRecordingManager
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustSerializer _seri = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly INetworkedMapManager _mapMan = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly INetConfigurationManager _netConf = default!;
    
    private ISawmill _sawmill = default!;
    private PVSSystem _pvs = default!;
    private List<object> _queuedMessages = new();

    private int _maxCompressedSize;
    private int _maxUncompressedSize;
    private int _tickBatchSize;
    private bool _enabled;
    private string _directory = string.Empty;

    public bool Recording => _curStream != null;
    private int _index = 0;
    private MemoryStream? _curStream;
    private int _currentCompressedSize;
    private int _currentUncompressedSize;
    private (GameTick Tick, TimeSpan Time) _recordingStart;
    private MappingDataNode? _yamlMetadata;
    private bool _firstTick = true;

    /// <inheritdoc/>
    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("replay");
        _pvs = _sysMan.GetEntitySystem<PVSSystem>();

        _netConf.OnValueChanged(CVars.ReplayEnabled, SetReplayEnabled, true);
        _netConf.OnValueChanged(CVars.ReplayMaxCompressedSize, (v) => _maxCompressedSize = v * 1024, true);
        _netConf.OnValueChanged(CVars.ReplayMaxUncompressedSize, (v) => _maxUncompressedSize = v * 1024, true);
        _netConf.OnValueChanged(CVars.ReplayTickBatchSize, (v) => _tickBatchSize = v * 1024, true);
    }

    private void SetReplayEnabled(bool value)
    {
        if (!value)
            StopRecording();

        _enabled = value;
    }

    /// <inheritdoc/>
    public void StopRecording()
    {
        if (_curStream == null)
            return;

        try
        {
            using var compressionContext = new ZStdCompressionContext();
            compressionContext.SetParameter(ZSTD_cParameter.ZSTD_c_compressionLevel, _netConf.GetCVar(CVars.NetPVSCompressLevel));
            WriteFile(compressionContext, continueRecording: false);
            _sawmill.Info("Replay recording stopped!");
        }
        catch
        {
            _curStream.Dispose();
            _curStream = null;
            _currentCompressedSize = 0;
            _currentUncompressedSize = 0;
            _index = 0;
            _firstTick = true;
            throw;
        }
    }

    /// <inheritdoc/>
    public void StartRecording()
    {
        if (!_enabled || _curStream != null)
            return;

        _directory = _netConf.GetCVar(CVars.ReplayDirectory);
        _curStream = new(_tickBatchSize * 2);
        _index = 0;
        _firstTick = true;
        WriteInitialMetadata();
        _recordingStart = (_timing.CurTick, _timing.CurTime);

        _sawmill.Info("Started recording replay...");
    }

    /// <inheritdoc/>
    public void ToggleRecording()
    {
        if (Recording)
            StopRecording();
        else
            StartRecording();
    }

    /// <inheritdoc/>
    public void QueueReplayMessage(object obj)
    {
        if (!Recording)
            return;

        DebugTools.Assert(obj.GetType().HasCustomAttribute<NetSerializableAttribute>());
        _queuedMessages.Add(obj);
    }

    /// <inheritdoc/>
    public void SaveReplayData(Thread mainThread, IDependencyCollection parentDeps, PvsThreadResources resource)
    {
        if (_curStream == null)
            return;

        try
        {
            if (mainThread != Thread.CurrentThread)
                IoCManager.InitThread(new DependencyCollection(parentDeps), true);

            var lastAck = _firstTick ? GameTick.Zero : _timing.CurTick - 1;

            var (entStates, deletions, _, __) = _pvs.GetAllEntityStates(null, lastAck, _timing.CurTick);
            var playerStates = _playerMan.GetPlayerStates(lastAck);
            var mapData = _mapMan.GetStateData(lastAck);

            var state = new GameState(lastAck, _timing.CurTick, 0,
                entStates, playerStates, deletions, mapData);

            _seri.SerializeDirect(_curStream, state);
            _seri.SerializeDirect(_curStream, new ReplayMessage() { Messages = _queuedMessages });
            _queuedMessages.Clear();

            if (_curStream.Length > _tickBatchSize)
                WriteFile(resource.CompressionContext);
        }
        catch (Exception e) 
        {
            _sawmill.Log(LogLevel.Error, e, "Caught exception while saving replay data.");
            StopRecording();
        }
    }

    private void WriteFile(ZStdCompressionContext compressionContext, bool continueRecording = true)
    {
        if (_curStream == null)
            return;

        _curStream.Position = 0;
        var filePath = (new ResourcePath(_directory) / $"{_index++}.dat").ToRootedPath();
        using var file = _resourceManager.UserData.OpenWrite(filePath);

        var buf = ArrayPool<byte>.Shared.Rent(ZStd.CompressBound((int)_curStream.Length));
        var length = compressionContext.Compress2(buf, _curStream.AsSpan());
        file.Write(BitConverter.GetBytes(length));
        file.Write(buf.AsSpan(0, length));
        ArrayPool<byte>.Shared.Return(buf);

        _currentUncompressedSize += (int)_curStream.Length;
        _currentCompressedSize += length;
        if (_currentUncompressedSize >= _maxUncompressedSize || _currentCompressedSize >= _maxCompressedSize)
        {
            _sawmill.Info("Reached max replay recording size. Stopping recording.");
            continueRecording = false;
        }

        if (continueRecording)
            _curStream.SetLength(0);
        else
        {
            WriteFinalMetadata();
            _curStream.Dispose();
            _curStream = null;
            _currentCompressedSize = 0;
            _currentUncompressedSize = 0;
            _index = 0;
            _firstTick = true;
        }
    }

    /// <summary>
    ///     Write general replay data required to read the rest of the replay. We write this at the beginning rather than at the end on the off-chance that something goes wrong along the way and the recording is incomplete.
    /// </summary>
    private void WriteInitialMetadata()
    {
        var dir = new ResourcePath(_directory).ToRootedPath();

        // Yeah just deleting the directory is shit, but so is just dumping replays to a folder like this rather than
        // some saner databse shit to actually make them publicly accessible.
        _resourceManager.UserData.Delete(dir);
        _resourceManager.UserData.CreateDir(dir);

        var (stringHash, stringData) = _seri.GetStringSerializerPackage();
        var extraData = new List<object>();

        // Saving YAML data
        {
            _yamlMetadata = new MappingDataNode();

            // TODO REPLAYS are these the right properties needed to uniquely identify content+engine builds?
            // could this just point to a github hash?
            _yamlMetadata["engineVersion"] = new ValueDataNode(_netConf.GetCVar(CVars.BuildEngineVersion));
            _yamlMetadata["buildForkId"] = new ValueDataNode(_netConf.GetCVar(CVars.BuildForkId));
            _yamlMetadata["buildVersion"] = new ValueDataNode(_netConf.GetCVar(CVars.BuildVersion));
            _yamlMetadata["buildHash"] = new ValueDataNode(_netConf.GetCVar(CVars.BuildHash));

            var timeBase = _timing.TimeBase;
            _yamlMetadata["typeHash"] =  new ValueDataNode(Convert.ToHexString(_seri.GetSerializableTypesHash()));
            _yamlMetadata["stringHash"] = new ValueDataNode(Convert.ToHexString(stringHash));
            _yamlMetadata["startTick"] = new ValueDataNode(_timing.CurTick.Value.ToString());
            _yamlMetadata["timeBaseTick"] = new ValueDataNode(timeBase.Item2.Value.ToString());
            _yamlMetadata["timeBaseTimespan"] = new ValueDataNode(timeBase.Item1.Ticks.ToString());

            StartingRecording?.Invoke((_yamlMetadata, extraData));

            var document = new YamlDocument(_yamlMetadata.ToYaml());
            using var ymlFile = _resourceManager.UserData.OpenWriteText(dir / "replay.yml");
            var stream = new YamlStream { document };
            stream.Save(new YamlMappingFix(new Emitter(ymlFile)), false);
        }

        // Saving misc extra data like networked messages that typically get sent to newly connecting clients.
        if (extraData.Count > 0)
        {
            using var initDataFile = _resourceManager.UserData.OpenWrite(dir / "init.dat");
            _seri.SerializeDirect(initDataFile, new ReplayMessage() { Messages = extraData });
        }

        // save data required for IRobustMappedStringSerializer
        using var stringFile = _resourceManager.UserData.OpenWrite(dir / "strings.dat");
        stringFile.Write(stringData);

        // Save replicated cvars.
        using var cvarsFile = _resourceManager.UserData.OpenWrite(dir / "cvars.toml");
        _netConf.SaveToTomlStream(cvarsFile, _netConf.GetReplicatedVars().Select(x => x.name));
    }

    private void WriteFinalMetadata()
    {
        if (_yamlMetadata == null)
            return;

        StoppingRecording?.Invoke(_yamlMetadata);
        var time = _timing.CurTime - _recordingStart.Time;
        _yamlMetadata["endTick"] = new ValueDataNode(_timing.CurTick.Value.ToString());
        _yamlMetadata["duration"] = new ValueDataNode(time.ToString());
        _yamlMetadata["fileCount"] = new ValueDataNode((_index+1).ToString());
        _yamlMetadata["size"] = new ValueDataNode(_currentCompressedSize.ToString());
        _yamlMetadata["uncompressedSize"] = new ValueDataNode(_currentUncompressedSize.ToString());

        // this just overwrites the previous yml with additional data.
        var dir = new ResourcePath(_directory).ToRootedPath();
        var document = new YamlDocument(_yamlMetadata.ToYaml());
        using var ymlFile = _resourceManager.UserData.OpenWriteText(dir / "replay.yml");
        var stream = new YamlStream { document };
        stream.Save(new YamlMappingFix(new Emitter(ymlFile)), false);
    }

    public (float Minutes, int Ticks, float Size, float UncompressedSize) GetReplayStats()
    {
        if (!Recording)
            return default;

        var time = (_timing.CurTime - _recordingStart.Time).TotalMinutes;
        var tick = _timing.CurTick.Value - _recordingStart.Tick.Value;
        var size = _currentCompressedSize / (1024f * 1024f);
        var altSize = _currentUncompressedSize / (1024f * 1024f);

        return ((float)time, (int)tick, size, altSize);
    }

    /// <inheritdoc/>
    public event Action<(MappingDataNode, List<object>)>? StartingRecording;

    /// <inheritdoc/>
    public event Action<MappingDataNode>? StoppingRecording;
}
