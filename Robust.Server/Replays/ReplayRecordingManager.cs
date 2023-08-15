using System;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Replays;
using Robust.Shared.Timing;

namespace Robust.Server.Replays;

internal sealed class ReplayRecordingManager : SharedReplayRecordingManager, IServerReplayRecordingManager
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    private GameTick _fromTick = GameTick.Zero;

    private PvsSystem _pvs = default!;

    public override void Initialize()
    {
        base.Initialize();
        _pvs = _sysMan.GetEntitySystem<PvsSystem>();
        NetConf.OnValueChanged(CVars.ReplayServerRecordingEnabled, SetReplayEnabled, true);
    }

    protected override string DefaultReplayFileName()
    {
        // UTC
        return DateTime.UtcNow.ToString(DefaultReplayNameFormat);
    }

    public override void RecordServerMessage(object obj)
        => RecordReplayMessage(obj);

    public override void RecordClientMessage(object obj)
    {
        // Do nothing.
    }

    public void Update()
    {
        if (!IsRecording)
        {
            UpdateWriteTasks();
            return;
        }

        var (entStates, deletions, _) = _pvs.GetAllEntityStates(null, _fromTick, Timing.CurTick);
        var playerStates = _player.GetPlayerStates(_fromTick);
        var state = new GameState(_fromTick, Timing.CurTick, 0, entStates, playerStates, deletions);
        _fromTick = Timing.CurTick;
        Update(state);
    }

    protected override void Reset()
    {
        base.Reset();
        _fromTick = GameTick.Zero;
    }
}
