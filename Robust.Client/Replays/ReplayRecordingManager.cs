using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.GameStates;
using Robust.Client.Player;
using Robust.Client.Timing;
using Robust.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Replays;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.Replays;

internal sealed class ReplayRecordingManager : SharedReplayRecordingManager
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IClientGameStateManager _state = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        NetConf.OnValueChanged(CVars.ReplayClientRecordingEnabled, SetReplayEnabled, true);
        _client.RunLevelChanged += OnRunLevelChanged;
        RecordingStarted += OnRecordingStarted;
    }

    private void OnRecordingStarted(MappingDataNode metadata, List<object> messages)
    {
        if (_player.LocalSession == null)
            return;

        // Add information about the user doing the recording. This is used to set the default replay observer position
        // when playing back the replay.
        var guid = _player.LocalUser.ToString();
        metadata[ReplayConstants.MetaKeyRecordedBy] = new ValueDataNode(guid);
    }

    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        // Replay recordings currently rely on the client receiving game states from a server.
        // single-player replays are not yet supported.
        if (e.OldLevel == ClientRunLevel.InGame)
            StopRecording();
    }

    public override bool CanStartRecording()
    {
        // Replay recordings currently rely on the client receiving game states from a server.
        // single-player replays are not yet supported.
        return base.CanStartRecording() && _client.RunLevel == ClientRunLevel.InGame;
    }

    public override void RecordClientMessage(object obj)
        => RecordReplayMessage(obj);

    protected override string DefaultReplayFileName()
    {
        // Local time
        return DateTime.Now.ToString(DefaultReplayNameFormat);
    }

    public override void RecordServerMessage(object obj)
    {
        // Do nothing.
    }

    public override bool TryStartRecording(
        IWritableDirProvider directory,
        string? name = null,
        bool overwrite = false,
        TimeSpan? duration = null,
        object? state = null)
    {
        if (!base.TryStartRecording(directory, name, overwrite, duration, state))
            return false;

        var (gameState, detachMsg) = CreateFullState();
        if (detachMsg != null)
            RecordReplayMessage(detachMsg);
        Update(gameState);
        return true;
    }

    private (GameState, ReplayMessage.LeavePvs?) CreateFullState()
    {
        var tick = _timing.LastRealTick;
        var players = _player.Sessions.Select(GetPlayerState).ToArray();
        var deletions = Array.Empty<NetEntity>();

        var fullRep = _state.GetFullRep();
        var entStates = new EntityState[fullRep.Count];
        var i = 0;
        foreach (var (netEntity, dict) in fullRep)
        {
            var compData = new ComponentChange[dict.Count];
            var netComps = new HashSet<ushort>(dict.Keys);
            var j = 0;
            foreach (var (id, compState) in dict)
            {
                compData[j++] = new ComponentChange(id, compState, tick);
            }

            entStates[i++] = new EntityState(netEntity, compData, tick, netComps);
        }

        var state = new GameState(
            GameTick.Zero,
            tick,
            default,
            entStates,
            players,
            deletions);

        var detached = new List<NetEntity>();
        var query = _entMan.AllEntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_entMan.IsClientSide(uid))
                continue;

            var nent = comp.NetEntity;
            DebugTools.Assert(fullRep.ContainsKey(nent));
            if ((comp.Flags & MetaDataFlags.Detached) != 0)
                detached.Add(nent);
        }

        var detachMsg = detached.Count > 0 ? new ReplayMessage.LeavePvs(detached, tick) : null;
        return (state, detachMsg);
    }

    private SessionState GetPlayerState(ICommonSession session)
    {
        return new SessionState
        {
            UserId = session.UserId,
            Status = session.Status,
            Name = session.Name,
            ControlledEntity = _entMan.GetNetEntity(session.AttachedEntity),
        };
    }
}
