using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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
using System.Globalization;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using static Robust.Server.GameStates.ServerGameStateManager;
using static Robust.Shared.Replays.IReplayRecordingManager;

namespace Robust.Server.Replays;

internal sealed class ReplayRecordingManager : IInternalReplayRecordingManager
{
    // date format for default replay names. Like the sortable template, but without colons.
    public const string DefaultReplayNameFormat = "yyyy-MM-dd_HH-mm-ss";

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustSerializer _seri = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly INetConfigurationManager _netConf = default!;

    private ISawmill _sawmill = default!;
    private PVSSystem _pvs = default!;
    private List<object> _queuedMessages = new();

    private int _maxCompressedSize;
    private int _maxUncompressedSize;
    private int _tickBatchSize;
    private bool _enabled;
    public bool Recording => _curStream != null;
    private int _index = 0;
    private MemoryStream? _curStream;
    private int _currentCompressedSize;
    private int _currentUncompressedSize;
    private (GameTick Tick, TimeSpan Time) _recordingStart;
    private TimeSpan? _recordingEnd;
    private MappingDataNode? _yamlMetadata;
    private bool _firstTick = true;
    private (IWritableDirProvider, ResPath)? _directory;

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
            _recordingEnd = null;
            _directory = null;
            throw;
        }
    }

    /// <inheritdoc/>
    public bool TryStartRecording(IWritableDirProvider directory, string? name = null, bool overwrite = false, TimeSpan? duration = null)
    {
        if (!_enabled || _curStream != null)
            return false;

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

        var basePath = new ResPath(_netConf.GetCVar(CVars.ReplayDirectory)).ToRootedPath();
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

        _curStream = new(_tickBatchSize * 2);
        _index = 0;
        _firstTick = true;
        _recordingStart = (_timing.CurTick, _timing.CurTime);

        try
        {
            WriteInitialMetadata(name);
        }
        catch
        {
            _directory = null;
            _curStream.Dispose();
            _curStream = null;
            _index = 0;
            _recordingStart = default;
            throw;
        }

        if (duration != null)
            _recordingEnd = _timing.CurTime + duration.Value;

        _sawmill.Info("Started recording replay...");
        return true;
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
    public void SaveReplayData(PvsThreadResources resource)
    {
        if (_curStream == null)
            return;

        try
        {
            var lastAck = _firstTick ? GameTick.Zero : _timing.CurTick - 1;
            _firstTick = false;

            var (entStates, deletions, _) = _pvs.GetAllEntityStates(null, lastAck, _timing.CurTick);
            var playerStates = _playerMan.GetPlayerStates(lastAck);
            var state = new GameState(lastAck, _timing.CurTick, 0, entStates, playerStates, deletions);

            _seri.SerializeDirect(_curStream, state);
            _seri.SerializeDirect(_curStream, new ReplayMessage() { Messages = _queuedMessages });
            _queuedMessages.Clear();

            bool continueRecording = _recordingEnd == null || _recordingEnd.Value >= _timing.CurTime;
            if (!continueRecording)
                _sawmill.Info("Reached requested replay recording length. Stopping recording.");

            if (!continueRecording || _curStream.Length > _tickBatchSize)
                WriteFile(resource.CompressionContext, continueRecording);
        }
        catch (Exception e)
        {
            _sawmill.Log(LogLevel.Error, e, "Caught exception while saving replay data.");
            StopRecording();
        }
    }

    private void WriteFile(ZStdCompressionContext compressionContext, bool continueRecording = true)
    {
        if (_curStream == null || _directory is not var (dir, path))
            return;

        _curStream.Position = 0;
        using var file = dir.OpenWrite(path / $"{_index++}.{Ext}");

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
            _recordingEnd = null;
            _directory = null;
        }
    }

    /// <summary>
    ///     Write general replay data required to read the rest of the replay. We write this at the beginning rather than at the end on the off-chance that something goes wrong along the way and the recording is incomplete.
    /// </summary>
    private void WriteInitialMetadata(string name)
    {
        if (_directory is not var (dir, path))
            return;

        var (stringHash, stringData) = _seri.GetStringSerializerPackage();
        var extraData = new List<object>();

        // Saving YAML data. This gets overwritten later anyways, this is mostly in case something goes wrong.
        {
            _yamlMetadata = new MappingDataNode();
            _yamlMetadata[Time] = new ValueDataNode(DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
            _yamlMetadata[Name] = new ValueDataNode(name);

            // version info
            _yamlMetadata[Engine] = new ValueDataNode(_netConf.GetCVar(CVars.BuildEngineVersion));
            _yamlMetadata[Fork] = new ValueDataNode(_netConf.GetCVar(CVars.BuildForkId));
            _yamlMetadata[ForkVersion] = new ValueDataNode(_netConf.GetCVar(CVars.BuildVersion));

            // Hash data
            _yamlMetadata[Hash] = new ValueDataNode(Convert.ToHexString(_seri.GetSerializableTypesHash()));
            _yamlMetadata[Strings] = new ValueDataNode(Convert.ToHexString(stringHash));
            _yamlMetadata[CompHash] = new ValueDataNode(Convert.ToHexString(_factory.GetHash(true)));

            // Time data
            var timeBase = _timing.TimeBase;
            _yamlMetadata[Tick] = new ValueDataNode(_recordingStart.Tick.Value.ToString());
            _yamlMetadata[BaseTick] = new ValueDataNode(timeBase.Item2.Value.ToString());
            _yamlMetadata[BaseTime] = new ValueDataNode(timeBase.Item1.Ticks.ToString());
            _yamlMetadata[ServerTime] = new ValueDataNode(_recordingStart.Time.ToString());

            OnRecordingStarted?.Invoke((_yamlMetadata, extraData));

            var document = new YamlDocument(_yamlMetadata.ToYaml());
            using var ymlFile = dir.OpenWriteText(path / MetaFile);
            var stream = new YamlStream { document };
            stream.Save(new YamlMappingFix(new Emitter(ymlFile)), false);
        }

        // Saving misc extra data like networked messages that typically get sent to newly connecting clients.
        // TODO compression
        if (extraData.Count > 0)
        {
            using var initDataFile = dir.OpenWrite(path / InitFile);
            _seri.SerializeDirect(initDataFile, new ReplayMessage() { Messages = extraData });
        }

        // save data required for IRobustMappedStringSerializer
        using var stringFile = dir.OpenWrite(path / StringsFile);
        stringFile.Write(stringData);

        // Save replicated cvars.
        using var cvarsFile = dir.OpenWrite(path / CvarFile);
        _netConf.SaveToTomlStream(cvarsFile, _netConf.GetReplicatedVars().Select(x => x.name));
    }

    private void WriteFinalMetadata()
    {
        if (_yamlMetadata == null || _directory is not var (dir, path))
            return;

        OnRecordingStopped?.Invoke(_yamlMetadata);
        var time = _timing.CurTime - _recordingStart.Time;
        _yamlMetadata[EndTick] = new ValueDataNode(_timing.CurTick.Value.ToString());
        _yamlMetadata[Duration] = new ValueDataNode(time.ToString());
        _yamlMetadata[FileCount] = new ValueDataNode(_index.ToString());
        _yamlMetadata[Compressed] = new ValueDataNode(_currentCompressedSize.ToString());
        _yamlMetadata[Uncompressed] = new ValueDataNode(_currentUncompressedSize.ToString());
        _yamlMetadata[EndTime] = new ValueDataNode(_timing.CurTime.ToString());

        // this just overwrites the previous yml with additional data.
        var document = new YamlDocument(_yamlMetadata.ToYaml());
        using var ymlFile = dir.OpenWriteText(path / MetaFile);
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
    public event Action<(MappingDataNode, List<object>)>? OnRecordingStarted;

    /// <inheritdoc/>
    public event Action<MappingDataNode>? OnRecordingStopped;
}
