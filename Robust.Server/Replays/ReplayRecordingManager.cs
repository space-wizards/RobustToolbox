using System;
using Robust.Server.GameStates;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Replays;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.Replays;

internal sealed class ReplayRecordingManager : SharedReplayRecordingManager, IServerReplayRecordingManager
{
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;

    private PvsSystem _pvs = default!;
    private PvsSession _pvsSession = new(default!, new ResizableMemoryRegion<PvsData>(1)) { DisableCulling = true };

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

        _pvs.ComputeSessionState(_pvsSession);
        Update(_pvsSession.State);
        _pvsSession.ClearState();
        _pvsSession.LastReceivedAck = Timing.CurTick;
    }

    protected override void Reset()
    {
        base.Reset();
        _pvsSession.LastReceivedAck = GameTick.Zero;
        _pvsSession.ClearState();
    }
}
