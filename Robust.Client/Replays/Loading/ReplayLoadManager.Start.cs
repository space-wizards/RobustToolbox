using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Robust.Client.GameStates;
using Robust.Shared.Timing;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Robust.Client.Replays.Loading;

public sealed partial class ReplayLoadManager
{
    public event Action<IWritableDirProvider, ResPath>? LoadOverride;

    public void LoadAndStartReplay(IWritableDirProvider dir, ResPath path)
    {
        if (LoadOverride != null)
            LoadOverride.Invoke(dir, path);
        else
            LoadAndStartReplayAsync(dir, path);
    }

    public async Task LoadAndStartReplayAsync(
        IWritableDirProvider dir,
        ResPath path,
        LoadReplayCallback? callback = null)
    {
        callback ??= (_, _, _, _) => Task.CompletedTask;
        var data = await LoadReplayAsync(dir, path, callback);
        await StartReplayAsync(data, callback);
    }

    public async Task StartReplayAsync(ReplayData data, LoadReplayCallback callback)
    {
        if (_client.RunLevel != ClientRunLevel.SinglePlayerGame)
            throw new Exception($"Invalid runlevel: {_client.RunLevel}.");

        if (_replayPlayback.Replay != null)
            throw new Exception("Already playing a replay");

        if (data.Checkpoints.Length == 0)
            return;

        _timing.Paused = true;
        var checkpoint = data.Checkpoints[0];
        data.CurrentIndex = checkpoint.Index;
        var state = checkpoint.State;

        foreach (var (name, value) in checkpoint.Cvars)
        {
            _confMan.SetCVar(name, value, force: true);
        }

        var tick = new GameTick(data.TickOffset.Value + (uint) data.CurrentIndex);
        _timing.CurTick = _timing.LastRealTick = _timing.LastProcessedTick = tick;

        _gameState.UpdateFullRep(state, cloneDelta: true);

        var i = 0;
        var total = state.EntityStates.Value.Count;
        List<EntityUid> entities = new(state.EntityStates.Value.Count);

        await callback(i, total, LoadingState.Spawning, true);
        foreach (var ent in state.EntityStates.Value)
        {
            var metaState = (MetaDataComponentState?)ent.ComponentChanges.Value?
                .FirstOrDefault(c => c.NetID == _metaId).State;
            if (metaState == null)
                throw new MissingMetadataException(ent.Uid);

            _entMan.CreateEntityUninitialized(metaState.PrototypeId, ent.Uid);
            entities.Add(ent.Uid);

            if (i++ % 50 == 0)
            {
                await callback(i, total, LoadingState.Spawning, false);
                _timing.CurTick = tick;
            }
        }

        await callback(0, total, LoadingState.Initializing, true);
        _gameState.ApplyGameState(state, data.NextState);

        i = 0;
        var query = _entMan.GetEntityQuery<MetaDataComponent>();
        foreach (var uid in entities)
        {
            _entMan.InitializeEntity(uid, query.GetComponent(uid));
            if (i++ % 50 == 0)
            {
                await callback(i, total, LoadingState.Initializing, false);
                _timing.CurTick = tick;
            }
        }

        i = 0;
        await callback(0, total, LoadingState.Starting, true);
        foreach (var uid in entities)
        {
            _entMan.StartEntity(uid);
            if (i++ % 50 == 0)
            {
                await callback(i, total, LoadingState.Starting, false);
                _timing.CurTick = tick;
            }
        }

        _timing.TimeBase = checkpoint.TimeBase;
        data.LastApplied = state.ToSequence;
        DebugTools.Assert(_timing.LastRealTick == tick);
        DebugTools.Assert(_timing.LastProcessedTick == tick);
        _timing.CurTick = tick + 1;
        data.CurrentIndex = 0;
        _replayPlayback.StartReplay(data);
        _timing.Paused = false;
    }
}
