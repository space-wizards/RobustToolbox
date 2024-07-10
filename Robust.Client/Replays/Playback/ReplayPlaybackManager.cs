using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Client.Audio;
using Robust.Client.Audio.Midi;
using Robust.Client.Configuration;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.Timing;
using Robust.Client.Upload;
using Robust.Shared;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Robust.Client.Replays.Playback;

internal sealed partial class ReplayPlaybackManager : IReplayPlaybackManager
{
    [Dependency] private readonly ILogManager _logMan = default!;
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IMidiManager _midi = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IAudioInternal _clydeAudio = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;
    [Dependency] private readonly IClientNetManager _netMan = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IGameController _controller = default!;
    [Dependency] private readonly IClientEntityManager _cEntManager = default!;
    [Dependency] private readonly ClientEntityManager _entMan = default!;
    [Dependency] private readonly IConfigurationManager _confMan = default!;
    [Dependency] private readonly NetworkResourceManager _netResMan = default!;
    [Dependency] private readonly IClientGameStateManager _gameState = default!;
    [Dependency] private readonly IClientNetConfigurationManager _netConf = default!;

    public event Action<MappingDataNode, List<object>>? ReplayPlaybackStarted;
    public event Action? ReplayPlaybackStopped;
    public event Action? ReplayPaused;
    public event Action? ReplayUnpaused;
    public event Action<(GameState Current, GameState? Next)>? BeforeApplyState;

    public ReplayData? Replay { get; private set; }
    public NetUserId? Recorder => Replay?.Recorder;
    private int _checkpointMinInterval;
    private int _replayMaxScrubTime;
    private int _visualEventThreshold;
    public uint? AutoPauseCountdown { get; set; }
    public int? ScrubbingTarget { get; set; }
    private bool _playing;
    private ushort _metaId;

    private bool _initialized;
    private ISawmill _sawmill = default!;
    private HashSet<Type> _warned = new();

    public bool Playing
    {
        get => Replay != null && _playing && ScrubbingTarget == null;
        set
        {
            if (Replay == null || _playing == value)
                return;

            _playing = value;

            if (_playing)
            {
                ReplayUnpaused?.Invoke();
            }
            else
            {
                StopAudio();
                AutoPauseCountdown = null;
                ReplayPaused?.Invoke();
            }
        }
    }

    public void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        _sawmill = _logMan.GetSawmill("replay");
        _metaId = _factory.GetRegistration(typeof(MetaDataComponent)).NetID!.Value;
        _confMan.OnValueChanged(CVars.CheckpointMinInterval, (value) => _checkpointMinInterval = value, true);
        _confMan.OnValueChanged(CVars.ReplayMaxScrubTime, (value) => _replayMaxScrubTime = value, true);
        _confMan.OnValueChanged(CVars.ReplaySkipThreshold, (value) => _visualEventThreshold = value, true);
        _client.RunLevelChanged += OnRunLevelChanged;
    }

    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (Replay == null || e.NewLevel == ClientRunLevel.SinglePlayerGame)
            return;

        _sawmill.Error($"Run level changed without stopping the current replay. Trace: {Environment.StackTrace}");
        StopReplay();
    }

    public void StartReplay(ReplayData replay)
    {
        if (Replay != null)
            throw new Exception("Already playing a replay");

        if (_client.RunLevel != ClientRunLevel.SinglePlayerGame)
            throw new Exception($"Invalid runlevel: {_client.RunLevel}.");

        Replay = replay;
        _controller.TickUpdateOverride += TickUpdateOverride;

        if (Replay.CurrentIndex < 0)
            ResetToNearestCheckpoint(0, true);

        ReplayPlaybackStarted?.Invoke(Replay.YamlData, Replay.InitialMessages?.Messages ?? new ());
    }

    public void StopReplay()
    {
        if (Replay == null)
            return;

        _playing = false;
        Replay.CurrentIndex = -1;
        Replay = null;
        _controller.TickUpdateOverride -= TickUpdateOverride;
        _entMan.FlushEntities();

        // Unload any uploaded prototypes & resources.
        _netResMan.ClearResources();
        _protoMan.Reset();

        ReplayPlaybackStopped?.Invoke();
    }

    public void StopAudio()
    {
        _clydeAudio.StopAllAudio();

        foreach (var renderer in _midi.Renderers)
        {
            renderer.ClearAllEvents();
            renderer.StopAllNotes();
        }
    }

    public bool TryGetRecorderEntity([NotNullWhen(true)] out EntityUid? uid)
    {
        if (Recorder != null
            && _player.SessionsDict.TryGetValue(Recorder.Value, out var session)
            && session.AttachedEntity is { } recorderEnt
            && _entMan.EntityExists(recorderEnt))
        {
            uid = recorderEnt;
            return true;
        }

        uid = null;
        return false;
    }
}
