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
    public event Action<IReplayFileReader>? LoadOverride;

    public async void  LoadAndStartReplay(IReplayFileReader fileReader)
    {
        if (LoadOverride != null)
            LoadOverride.Invoke(fileReader);
        else
            await LoadAndStartReplayAsync(fileReader);
    }

    public async Task LoadAndStartReplayAsync(
        IReplayFileReader fileReader,
        LoadReplayCallback? callback = null)
    {
        callback ??= (_, _, _, _) => Task.CompletedTask;
        var data = await LoadReplayAsync(fileReader, callback);
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

        foreach (var (name, value) in checkpoint.Cvars)
        {
            _confMan.SetCVar(name, value, force: true);
        }

        var tick = new GameTick(data.TickOffset.Value + (uint) data.CurrentIndex);
        _timing.CurTick = _timing.LastRealTick = _timing.LastProcessedTick = tick;

        _gameState.UpdateFullRep(checkpoint.FullState, cloneDelta: true);

        var i = 0;
        var entStates = checkpoint.FullState.EntityStates.Value;
        var total = entStates.Count;
        List<EntityUid> entities = new(total);

        await callback(i, total, LoadingState.Spawning, true);
        foreach (var ent in entStates)
        {
            var metaState = (MetaDataComponentState?)ent.ComponentChanges.Value?
                .FirstOrDefault(c => c.NetID == _metaId).State;
            if (metaState == null)
                throw new MissingMetadataException(ent.NetEntity);

            var uid = _entMan.CreateEntityUninitialized(metaState.PrototypeId);
            entities.Add(uid);
            var metaComp = _entMan.GetComponent<MetaDataComponent>(uid);

            // Client creates a client-side net entity for the newly created entity.
            // We need to clear this mapping before assigning the real net id.
            // TODO NetEntity Jank: prevent the client from creating this in the first place.
            _entMan.ClearNetEntity(metaComp.NetEntity);

            _entMan.SetNetEntity(uid, ent.NetEntity, metaComp);

            if (i++ % 50 == 0)
            {
                await callback(i, total, LoadingState.Spawning, false);
                _timing.CurTick = tick;
            }
        }

        // TODO add progress bar / loading stage for this?
        await callback(0, total, LoadingState.Initializing, true);
        var nextIndex = checkpoint.Index + 1;
        var next =  nextIndex < data.States.Count ? data.States[nextIndex] : null;
        _gameState.ClearDetachQueue();
        _gameState.ApplyGameState(checkpoint.State, next);

        // Sort entities to ensure that we initialize parents before children
        var sorted = new List<EntityUid>(entities.Count);
        var added = new HashSet<EntityUid>(entities.Count);
        var xformQuery = _entMan.GetEntityQuery<TransformComponent>();
        foreach (var uid in entities)
        {
            AddSorted(uid, sorted, added, xformQuery);
        }
        DebugTools.AssertEqual(sorted.Count, entities.Count);
        DebugTools.AssertEqual(added.Count, entities.Count);
        await callback(i, total, LoadingState.Initializing, false);

        i = 0;
        var query = _entMan.GetEntityQuery<MetaDataComponent>();
        foreach (var uid in sorted)
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
        foreach (var uid in sorted)
        {
            _entMan.StartEntity(uid);
            if (i++ % 50 == 0)
            {
                await callback(i, total, LoadingState.Starting, false);
                _timing.CurTick = tick;
            }
        }

        // TODO add progress bar / loading stage for this?
        _gameState.ClearDetachQueue();
        _gameState.DetachImmediate(checkpoint.Detached);

        _timing.TimeBase = checkpoint.TimeBase;
        data.LastApplied = checkpoint.Tick;
        DebugTools.Assert(_timing.LastRealTick == tick);
        DebugTools.Assert(_timing.LastProcessedTick == tick);
        _timing.CurTick = tick + 1;
        data.CurrentIndex = 0;
        _replayPlayback.StartReplay(data);
        _timing.Paused = false;
    }

    private void AddSorted(EntityUid uid, List<EntityUid> sortedList, HashSet<EntityUid> added, EntityQuery<TransformComponent> query)
    {
        if (!added.Add(uid))
            return;

        var parent = query.Comp(uid).ParentUid;
        if (parent != EntityUid.Invalid)
            AddSorted(parent, sortedList, added, query);

        sortedList.Add(uid);
    }
}
