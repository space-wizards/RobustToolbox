// ReSharper disable once RedundantUsingDirective
// Used in EXCEPTION_TOLERANCE preprocessor
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.Timing;
using Robust.Shared;
using Robust.Shared.Configuration;
#if EXCEPTION_TOLERANCE
using Robust.Shared.Exceptions;
#endif
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Profiling;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameStates
{
    /// <inheritdoc />
    [UsedImplicitly]
    public sealed class ClientGameStateManager : IClientGameStateManager
    {
        private GameStateProcessor _processor = default!;

        private uint _nextInputCmdSeq = 1;
        private readonly Queue<FullInputCmdMessage> _pendingInputs = new();

        private readonly Queue<(uint sequence, GameTick sourceTick, EntityEventArgs msg, object sessionMsg)>
            _pendingSystemMessages
                = new();

        private uint _metaCompNetId;

        [Dependency] private readonly IComponentFactory _compFactory = default!;
        [Dependency] private readonly IClientEntityManagerInternal _entities = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IClientNetManager _network = default!;
        [Dependency] private readonly IBaseClient _client = default!;
        [Dependency] private readonly INetworkedMapManager _mapManager = default!;
        [Dependency] private readonly IClientGameTiming _timing = default!;
        [Dependency] private readonly INetConfigurationManager _config = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly ClientEntityManager _entityManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly ProfManager _prof = default!;
#if EXCEPTION_TOLERANCE
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
#endif

        private ISawmill _sawmill = default!;

        /// <inheritdoc />
        public int MinBufferSize => _processor.MinBufferSize;

        /// <inheritdoc />
        public int TargetBufferSize => _processor.TargetBufferSize;

        /// <inheritdoc />
        public int CurrentBufferSize => _processor.CalculateBufferSize(_timing.LastRealTick);

        public bool IsPredictionEnabled { get; private set; }
        public bool PredictionNeedsResetting { get; private set; }

        public int PredictTickBias { get; private set; }
        public float PredictLagBias { get; private set; }

        public int StateBufferMergeThreshold { get; private set; }

        private uint _lastProcessedInput;

        /// <summary>
        ///     Maximum number of entities that are sent to null-space each tick due to leaving PVS.
        /// </summary>
        private int _pvsDetachBudget;

        /// <inheritdoc />
        public event Action<GameStateAppliedArgs>? GameStateApplied;

        public event Action<MsgStateLeavePvs>? PvsLeave;

        /// <inheritdoc />
        public void Initialize()
        {
            _sawmill = Logger.GetSawmill(CVars.NetPredict.Name);
            _processor = new GameStateProcessor(_timing);

            _network.RegisterNetMessage<MsgState>(HandleStateMessage);
            _network.RegisterNetMessage<MsgStateLeavePvs>(HandlePvsLeaveMessage);
            _network.RegisterNetMessage<MsgStateAck>();
            _network.RegisterNetMessage<MsgStateRequestFull>();
            _client.RunLevelChanged += RunLevelChanged;

            _config.OnValueChanged(CVars.NetInterp, b => _processor.Interpolation = b, true);
            _config.OnValueChanged(CVars.NetBufferSize, i => _processor.BufferSize = i, true);
            _config.OnValueChanged(CVars.NetLogging, b => _processor.Logging = b, true);
            _config.OnValueChanged(CVars.NetPredict, b => IsPredictionEnabled = b, true);
            _config.OnValueChanged(CVars.NetPredictTickBias, i => PredictTickBias = i, true);
            _config.OnValueChanged(CVars.NetPredictLagBias, i => PredictLagBias = i, true);
            _config.OnValueChanged(CVars.NetStateBufMergeThreshold, i => StateBufferMergeThreshold = i, true);
            _config.OnValueChanged(CVars.NetPVSEntityExitBudget, i => _pvsDetachBudget = i, true);

            _processor.Interpolation = _config.GetCVar(CVars.NetInterp);
            _processor.BufferSize = _config.GetCVar(CVars.NetBufferSize);
            _processor.Logging = _config.GetCVar(CVars.NetLogging);
            IsPredictionEnabled = _config.GetCVar(CVars.NetPredict);
            PredictTickBias = _config.GetCVar(CVars.NetPredictTickBias);
            PredictLagBias = _config.GetCVar(CVars.NetPredictLagBias);

            var metaId = _compFactory.GetRegistration(typeof(MetaDataComponent)).NetID;
            if (!metaId.HasValue)
                throw new InvalidOperationException("MetaDataComponent does not have a NetId.");

            _metaCompNetId = metaId.Value;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _processor.Reset();
            _timing.CurTick = GameTick.Zero;
            _timing.LastRealTick = GameTick.Zero;
            _lastProcessedInput = 0;
        }

        private void RunLevelChanged(object? sender, RunLevelChangedEventArgs args)
        {
            if (args.NewLevel == ClientRunLevel.Initialize)
            {
                // We JUST left a server or the client started up, Reset everything.
                Reset();
            }
        }

        public void InputCommandDispatched(FullInputCmdMessage message)
        {
            if (!IsPredictionEnabled)
            {
                return;
            }

            message.InputSequence = _nextInputCmdSeq;
            _pendingInputs.Enqueue(message);

            _inputManager.NetworkBindMap.TryGetKeyFunction(message.InputFunctionId, out var boundFunc);
            _sawmill.Debug(
                $"CL> SENT tick={_timing.CurTick}, sub={_timing.TickFraction}, seq={_nextInputCmdSeq}, func={boundFunc.FunctionName}, state={message.State}");
            _nextInputCmdSeq++;
        }

        public uint SystemMessageDispatched<T>(T message) where T : EntityEventArgs
        {
            if (!IsPredictionEnabled)
            {
                return default;
            }

            DebugTools.AssertNotNull(_players.LocalPlayer);

            var evArgs = new EntitySessionEventArgs(_players.LocalPlayer!.Session);
            _pendingSystemMessages.Enqueue((_nextInputCmdSeq, _timing.CurTick, message,
                new EntitySessionMessage<T>(evArgs, message)));

            return _nextInputCmdSeq++;
        }

        private void HandleStateMessage(MsgState message)
        {
            // We ONLY ack states that are definitely going to get applied. Otherwise the sever might assume that we
            // applied a state containing entity-creation information, which it would then no longer send to us when
            // we re-encounter this entity
            if (_processor.AddNewState(message.State))
                AckGameState(message.State.ToSequence);
        }

        private void HandlePvsLeaveMessage(MsgStateLeavePvs message)
        {
            _processor.AddLeavePvsMessage(message);
            PvsLeave?.Invoke(message);
        }

        /// <inheritdoc />
        public void ApplyGameState()
        {
            // Calculate how many states we need to apply this tick.
            // Always at least one, but can be more based on StateBufferMergeThreshold.
            var curBufSize = CurrentBufferSize;
            var targetBufSize = TargetBufferSize;

            var bufferOverflow = curBufSize - targetBufSize - StateBufferMergeThreshold;
            var targetProccessedTick = (bufferOverflow > 1)
                ? _timing.LastProcessedTick + (uint)bufferOverflow
                : _timing.LastProcessedTick + 1;
            
            _prof.WriteValue($"State buffer size", curBufSize);
            _prof.WriteValue($"State apply count", targetProccessedTick.Value - _timing.LastProcessedTick.Value);

            bool processedAny = false;

            _timing.LastProcessedTick = _timing.LastRealTick;
            while (_timing.LastProcessedTick < targetProccessedTick)
            {
                // TODO: We could theoretically communicate with the GameStateProcessor better here.
                // Since game states are sliding windows, it is possible that we need less than applyCount applies here.
                // Consider, if you have 3 states, (tFrom=1, tTo=2), (tFrom=1, tTo=3), (tFrom=2, tTo=3),
                // you only need to apply the last 2 states to go from 1 -> 3.
                // instead of all 3.
                // This would be a nice optimization though also minor since the primary cost here
                // is avoiding entity system and re-prediction runs.
                //
                // Note however that it is possible that some state (e.g. 1->2) contains information for entity creation
                // for some entity that has left pvs by tick 3. Given that state 1->2 was acked, the server will not
                // re-send that creation data later. So if we skip it and only apply tick 1->3, that will lead to a missing
                // meta-data error. So while this can still be optimized, its probably not worth the headache.

                if (!_processor.TryGetServerState(out var curState, out var nextState))
                {
                    // Might just me missing a state, but we may be able to make use of a future state if it has a low enough from sequence.
                    break;
                }

                processedAny = true;

                if (curState == null)
                {
                    _timing.LastProcessedTick += 1;
                    continue;
                }

                if (PredictionNeedsResetting)
                    ResetPredictedEntities();

                // If we were waiting for a new state, we are now applying it.
                if (_processor.LastFullStateRequested.HasValue)
                {
                    _processor.LastFullStateRequested = null;
                    _timing.LastProcessedTick = curState.ToSequence;
                }
                else
                    _timing.LastProcessedTick += 1;

                _timing.CurTick = _timing.LastRealTick = _timing.LastProcessedTick;

                // Update the cached server state.
                using (_prof.Group("FullRep"))
                {
                    _processor.UpdateFullRep(curState);
                }

                IEnumerable<EntityUid> createdEntities;
                using (_prof.Group("ApplyGameState"))
                {
                    if (_timing.LastProcessedTick < targetProccessedTick && nextState != null)
                    {
                        // We are about to apply another state after this one anyways. So there is no need to pass in
                        // the next state for frame interpolation. Really, if we are applying 3 or more states, we
                        // should be checking the next-next state and so on.
                        //
                        // Basically: we only need to apply next-state for the last cur-state we are applying. but 99%
                        // of the time, we are only applying a single tick. But if we are applying more than one the
                        // client tends to stutter, so this sort of matters.
                        nextState = null;
                    }

#if EXCEPTION_TOLERANCE
                    try
                    {
#endif
                    createdEntities = ApplyGameState(curState, nextState);
#if EXCEPTION_TOLERANCE
                    }
                    catch (Exception e)
                    {
                        // Something has gone wrong. Probably a missing meta-data component.
                        Logger.Error("Something has gone wrong while applying a game state. Perhaps a full server state will fix it.");
                        RequestFullState();
                        throw;
                    }
#endif
                }

                using (_prof.Group("MergeImplicitData"))
                {
                    MergeImplicitData(createdEntities);
                }

                if (_lastProcessedInput < curState.LastProcessedInput)
                {
                    _sawmill.Debug($"SV> RCV  tick={_timing.CurTick}, last processed ={_lastProcessedInput}");
                    _lastProcessedInput = curState.LastProcessedInput;
                }
            }

            // Slightly speed up or slow down the client tickrate based on the contents of the buffer.
            // TryGetTickStates should have cleaned out any old states, so the buffer contains [t+0(cur), t+1(next), t+2, t+3, ..., t+n]
            // we can use this info to properly time our tickrate so it does not run fast or slow compared to the server.
            if (_processor.WaitingForFull)
                _timing.TickTimingAdjustment = 0f;
            else
                _timing.TickTimingAdjustment = (CurrentBufferSize - (float)TargetBufferSize) * 0.10f;

            // If we are about to process an another tick in the same frame, lets not bother unnecessarily running prediction ticks
            // Really the main-loop ticking just needs to be more specialized for clients.
            if (_timing.TickRemainder >= _timing.CalcAdjustedTickPeriod())
                return;

            if (!processedAny)
            {
                // Failed to process even a single tick. Chances are the tick buffer is empty, either because of
                // networking issues or because the server is dead. This will functionally freeze the client-side simulation.
                return;
            }

            // remove old pending inputs
            while (_pendingInputs.Count > 0 && _pendingInputs.Peek().InputSequence <= _lastProcessedInput)
            {
                var inCmd = _pendingInputs.Dequeue();

                _inputManager.NetworkBindMap.TryGetKeyFunction(inCmd.InputFunctionId, out var boundFunc);
                _sawmill.Debug($"SV>     seq={inCmd.InputSequence}, func={boundFunc.FunctionName}, state={inCmd.State}");
            }

            while (_pendingSystemMessages.Count > 0 && _pendingSystemMessages.Peek().sequence <= _lastProcessedInput)
            {
                _pendingSystemMessages.Dequeue();
            }

            DebugTools.Assert(_timing.InSimulation);

            var ping = (_network.ServerChannel?.Ping ?? 0) / 1000f + PredictLagBias; // seconds.
            var predictionTarget = _timing.LastProcessedTick + (uint) (_processor.TargetBufferSize + Math.Ceiling(_timing.TickRate * ping) + PredictTickBias);

            if (IsPredictionEnabled)
            {
                PredictionNeedsResetting = true;
                PredictTicks(predictionTarget);
            }

            using (_prof.Group("Tick"))
            {
                _entities.TickUpdate((float) _timing.TickPeriod.TotalSeconds, noPredictions: !IsPredictionEnabled);
            }
        }

        public void RequestFullState()
        {
            Logger.Info("Requesting full server state");
            _network.ClientSendMessage(new MsgStateRequestFull() { Tick = _timing.LastRealTick });
            _processor.RequestFullState();
        }

        public void PredictTicks(GameTick predictionTarget)
        {
            using var _p = _prof.Group("Prediction");
            using var _ = _timing.StartPastPredictionArea();

            if (_pendingInputs.Count > 0)
            {
                _sawmill.Debug("CL> Predicted:");
            }

            var input = _entitySystemManager.GetEntitySystem<InputSystem>();
            var pendingInputEnumerator = _pendingInputs.GetEnumerator();
            var pendingMessagesEnumerator = _pendingSystemMessages.GetEnumerator();
            var hasPendingInput = pendingInputEnumerator.MoveNext();
            var hasPendingMessage = pendingMessagesEnumerator.MoveNext();

            while (_timing.CurTick < predictionTarget)
            {
                _timing.CurTick += 1;
                var groupStart = _prof.WriteGroupStart();

                while (hasPendingInput && pendingInputEnumerator.Current.Tick <= _timing.CurTick)
                {
                    var inputCmd = pendingInputEnumerator.Current;

                    _inputManager.NetworkBindMap.TryGetKeyFunction(inputCmd.InputFunctionId, out var boundFunc);

                    _sawmill.Debug(
                        $"    seq={inputCmd.InputSequence}, sub={inputCmd.SubTick}, dTick={_timing.CurTick}, func={boundFunc.FunctionName}, " +
                        $"state={inputCmd.State}");

                    input.PredictInputCommand(inputCmd);
                    hasPendingInput = pendingInputEnumerator.MoveNext();
                }

                while (hasPendingMessage && pendingMessagesEnumerator.Current.sourceTick <= _timing.CurTick)
                {
                    var msg = pendingMessagesEnumerator.Current.msg;

                    _entities.EventBus.RaiseEvent(EventSource.Local, msg);
                    _entities.EventBus.RaiseEvent(EventSource.Local, pendingMessagesEnumerator.Current.sessionMsg);
                    hasPendingMessage = pendingMessagesEnumerator.MoveNext();
                }

                if (_timing.CurTick != predictionTarget)
                {
                    using (_prof.Group("Systems"))
                    {
                        // Don't run EntitySystemManager.TickUpdate if this is the target tick,
                        // because the rest of the main loop will call into it with the target tick later,
                        // and it won't be a past prediction.
                        _entitySystemManager.TickUpdate((float)_timing.TickPeriod.TotalSeconds, noPredictions: false);
                    }

                    using (_prof.Group("Event queue"))
                    {
                        ((IBroadcastEventBusInternal)_entities.EventBus).ProcessEventQueue();
                    }
                }

                _prof.WriteGroupEnd(groupStart, "Prediction tick", ProfData.Int64(_timing.CurTick.Value));
            }
        }

        private void ResetPredictedEntities()
        {
            PredictionNeedsResetting = false;

            using var _ = _prof.Group("ResetPredictedEntities");
            using var __ = _timing.StartPastPredictionArea();
            using var ___ = _timing.StartStateApplicationArea();

            var countReset = 0;
            var system = _entitySystemManager.GetEntitySystem<ClientDirtySystem>();
            var query = _entityManager.GetEntityQuery<MetaDataComponent>();

            // This is terrible, and I hate it.
            _entitySystemManager.GetEntitySystem<SharedGridTraversalSystem>().QueuedEvents.Clear();

            foreach (var entity in system.GetDirtyEntities())
            {
                // Check log level first to avoid the string alloc.
                if (_sawmill.Level <= LogLevel.Debug)
                    _sawmill.Debug($"Entity {entity} was made dirty.");

                if (!_processor.TryGetLastServerStates(entity, out var last))
                {
                    // Entity was probably deleted on the server so do nothing.
                    continue;
                }

                countReset += 1;

                // TODO: handle component deletions/creations.
                foreach (var (netId, comp) in _entityManager.GetNetComponents(entity))
                {
                    DebugTools.AssertNotNull(netId);

                    if (comp.LastModifiedTick <= _timing.LastRealTick || !last.TryGetValue(netId, out var compState))
                    {
                        continue;
                    }

                    if (_sawmill.Level <= LogLevel.Debug)
                        _sawmill.Debug($"  And also its component {comp.GetType()}");

                    // TODO: Handle interpolation.
                    var handleState = new ComponentHandleState(compState, null);
                    _entities.EventBus.RaiseComponentEvent(comp, ref handleState);
                    comp.HandleComponentState(compState, null);
                    comp.LastModifiedTick = _timing.LastRealTick;
                }
                var meta = query.GetComponent(entity);
                DebugTools.Assert(meta.LastModifiedTick > _timing.LastRealTick || meta.LastModifiedTick == GameTick.Zero);
                meta.EntityLastModifiedTick = _timing.LastRealTick;
            }

            system.Reset();

            _prof.WriteValue("Reset count", ProfData.Int32(countReset));
        }

        /// <summary>
        ///     Infer implicit state data for newly created entities.
        /// </summary>
        /// <remarks>
        ///     Whenever a new entity is created, the server doesn't send full state data, given that much of the data
        ///     can simply be obtained from the entity prototype information. This function basically creates a fake
        ///     initial server state for any newly created entity. It does this by simply using the standard <see
        ///     cref="IEntityManager.GetComponentState(IEventBus, IComponent)"/>.
        /// </remarks>
        private void MergeImplicitData(IEnumerable<EntityUid> createdEntities)
        {
            var outputData = new Dictionary<EntityUid, Dictionary<ushort, ComponentState>>();
            var bus = _entityManager.EventBus;

            foreach (var createdEntity in createdEntities)
            {
                var compData = new Dictionary<ushort, ComponentState>();
                outputData.Add(createdEntity, compData);

                foreach (var (netId, component) in _entityManager.GetNetComponents(createdEntity))
                {
                    if (component.NetSyncEnabled)
                        compData.Add(netId, _entityManager.GetComponentState(bus, component));
                }
            }

            _processor.MergeImplicitData(outputData);
        }

        private void AckGameState(GameTick sequence)
        {
            _network.ClientSendMessage(new MsgStateAck() { Sequence = sequence });
        }

        private IEnumerable<EntityUid> ApplyGameState(GameState curState, GameState? nextState)
        {
            using var _ = _timing.StartStateApplicationArea();

            using (_prof.Group("Config"))
            {
                _config.TickProcessMessages();
            }

            using (_prof.Group("Map Pre"))
            {
                _mapManager.ApplyGameStatePre(curState.MapData, curState.EntityStates.Span);
            }

            (IEnumerable<EntityUid> Created, List<EntityUid> Detached) output;
            using (_prof.Group("Entity"))
            {
                output = ApplyEntityStates(curState, nextState);
            }

            using (_prof.Group("Player"))
            {
                _players.ApplyPlayerStates(curState.PlayerStates.Value ?? Array.Empty<PlayerState>());
            }

            using (_prof.Group("Callback"))
            {
                GameStateApplied?.Invoke(new GameStateAppliedArgs(curState, output.Detached));
            }

            return output.Created;
        }

        private (IEnumerable<EntityUid> Created, List<EntityUid> Detached) ApplyEntityStates(GameState curState, GameState? nextState)
        {
            var metas = _entities.GetEntityQuery<MetaDataComponent>();
            var xforms = _entities.GetEntityQuery<TransformComponent>();

            var toApply = new Dictionary<EntityUid, (bool EnteringPvs, GameTick LastApplied, EntityState? curState, EntityState? nextState)>();
            var toCreate = new Dictionary<EntityUid, EntityState>();
            var enteringPvs = 0;

            var curSpan = curState.EntityStates.Span;
            foreach (var es in curSpan)
            {
                if (!metas.TryGetComponent(es.Uid, out var meta))
                {
                    toCreate.Add(es.Uid, es);
                    continue;
                }

                bool isEnteringPvs = (meta.Flags & MetaDataFlags.Detached) != 0;
                if (isEnteringPvs)
                {
                    meta.Flags &= ~MetaDataFlags.Detached;
                    enteringPvs++;
                }
                else if (meta.LastStateApplied >= es.EntityLastModified && meta.LastStateApplied != GameTick.Zero)
                {
                    meta.LastStateApplied = curState.ToSequence;
                    continue;
                }
                
                toApply.Add(es.Uid, (isEnteringPvs, meta.LastStateApplied, es, null));
                meta.LastStateApplied = curState.ToSequence;
            }

            // Create new entities
            if (toCreate.Count > 0)
            {
                using var _ = _prof.Group("Create uninitialized entities");
                _prof.WriteValue("Count", ProfData.Int32(toCreate.Count));

                foreach (var (uid, es) in toCreate)
                {
                    var metaState = (MetaDataComponentState?)es.ComponentChanges.Value?.FirstOrDefault(c => c.NetID == _metaCompNetId).State;
                    if (metaState == null)
                        throw new InvalidOperationException($"Server sent new entity state for {uid} without metadata component!");

                    _entities.CreateEntity(metaState.PrototypeId, uid);
                    toApply.Add(uid, (false, GameTick.Zero, es, null));

                    var newMeta = metas.GetComponent(uid);
                    newMeta.LastStateApplied = curState.ToSequence;
                }
            }

            // Detatch entities to null space
            var xformSys = _entitySystemManager.GetEntitySystem<SharedTransformSystem>();
            var detached = ProcessPvsDeparture(curState.ToSequence, metas, xforms, xformSys);

            // Check next state (AFTER having created new entities introduced in curstate)
            if (nextState != null)
            {
                foreach (var es in nextState.EntityStates.Span)
                {
                    var uid = es.Uid;

                    if (!metas.TryGetComponent(uid, out var meta))
                        continue;

                    // Does the next state actually have any future information about this entity that could be used for interpolation?
                    if (es.EntityLastModified != nextState.ToSequence)
                        continue;

                    if (toApply.TryGetValue(uid, out var state))
                        toApply[uid] = (state.EnteringPvs, state.LastApplied, state.curState, es);
                    else
                        toApply[uid] = (false, GameTick.Zero, null, es);
                }
            }

            // Apply entity states.
            using (_prof.Group("Apply States"))
            {
                foreach (var (entity, data) in toApply)
                {
                    HandleEntityState(entity, _entities.EventBus, data.curState,
                        data.nextState, data.LastApplied, curState.ToSequence, data.EnteringPvs);
                }
                _prof.WriteValue("Count", ProfData.Int32(toApply.Count));
            }

            var delSpan = curState.EntityDeletions.Span;
            if (delSpan.Length > 0)
                ProcessDeletions(delSpan, xforms, metas, xformSys);

            // Initialize and start the newly created entities.
            if (toCreate.Count > 0)
                InitializeAndStart(toCreate);

            _prof.WriteValue("State Size", ProfData.Int32(curSpan.Length));
            _prof.WriteValue("Entered PVS", ProfData.Int32(enteringPvs));

            return (toCreate.Keys, detached);
        }

        private void ProcessDeletions(
            ReadOnlySpan<EntityUid> delSpan,
            EntityQuery<TransformComponent> xforms,
            EntityQuery<MetaDataComponent> metas,
            SharedTransformSystem xformSys)
        {
            // Processing deletions is non-trivial, because by default deletions will also delete all child entities.
            // 
            // Naively: easy, just apply server states to process any transform states before deleting, right? But now
            // that PVS detach messages are sent separately & processed over time, the entity may have left our view,
            // but not yet been moved to null-space. In that case, the server would not send us transform states, and
            // deleting an entity could falsely delete the children as well. Therefore, before deleting we must detach
            // all children to null. This also gets called WHILE deleting, but we need to do it beforehand. Given that
            // they are either also about to get deleted, or about to be send to out-of-pvs null-space, this shouldn't
            // be a significant performance impact.

            using var _ = _prof.Group("Deletion");

            foreach (var id in delSpan)
            {
                if (!xforms.TryGetComponent(id, out var xform))
                    continue; // Already deleted? or never sent to us?

                // First, a single recursive map change
                xformSys.DetachParentToNull(xform, xforms, metas);

                // Then detach all children.
                var childEnumerator = xform.ChildEnumerator;
                while (childEnumerator.MoveNext(out var child))
                {
                    xformSys.DetachParentToNull(xforms.GetComponent(child.Value), xforms, metas, xform);
                }

                // Finally, delete the entity.
                _entities.DeleteEntity(id);
            }
            _prof.WriteValue("Count", ProfData.Int32(delSpan.Length));
        }

        private List<EntityUid> ProcessPvsDeparture(GameTick toTick, EntityQuery<MetaDataComponent> metas, EntityQuery<TransformComponent> xforms, SharedTransformSystem xformSys)
        {
            var toDetach = _processor.GetEntitiesToDetach(toTick, _pvsDetachBudget);
            var detached = new List<EntityUid>();

            if (toDetach.Count == 0)
                return detached;

            // TODO optimize
            // If an entity is leaving PVS, so are all of its children. If we can preserve the hierarchy we can avoid
            // things like container insertion and ejection.

            using var _ = _prof.Group("Leave PVS");

            foreach (var (tick, ents) in toDetach)
            {
                foreach (var ent in ents)
                {
                    if (!metas.TryGetComponent(ent, out var meta))
                        continue;

                    if (meta.LastStateApplied > tick)
                    {
                        // Server sent a new state for this entity sometime after the detach message was sent. The
                        // detach message probably just arrived late or was initially dropped.
                        continue;
                    }

                    if ((meta.Flags & MetaDataFlags.Detached) != 0)
                        continue;

                    meta.Flags |= MetaDataFlags.Detached;
                    meta.LastStateApplied = toTick;

                    var xform = xforms.GetComponent(ent);
                    if (xform.ParentUid.IsValid())
                        xformSys.DetachParentToNull(xform, xforms, metas);
                    detached.Add(ent);
                }
            }

            _prof.WriteValue("Count", ProfData.Int32(detached.Count));
            return detached;
        }

        private void InitializeAndStart(Dictionary<EntityUid, EntityState> toCreate)
        {
#if EXCEPTION_TOLERANCE
            HashSet<EntityUid> brokenEnts = new HashSet<EntityUid>();
#endif
            using (_prof.Group("Initialize Entity"))
            {
                foreach (var entity in toCreate.Keys)
                {
#if EXCEPTION_TOLERANCE
                    try
                    {
#endif
                    _entities.InitializeEntity(entity);
#if EXCEPTION_TOLERANCE
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("state", $"Server entity threw in Init: ent={_entityManager.ToPrettyString(entity)}\n{e}");
                        brokenEnts.Add(entity);
                        toCreate.Remove(entity);
                    }
#endif
                }
            }

            using (_prof.Group("Start Entity"))
            {
                foreach (var entity in toCreate.Keys)
                {
#if EXCEPTION_TOLERANCE
                    try
                    {
#endif
                    _entities.StartEntity(entity);
#if EXCEPTION_TOLERANCE
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("state", $"Server entity threw in Start: ent={_entityManager.ToPrettyString(entity)}\n{e}");
                        brokenEnts.Add(entity);
                        toCreate.Remove(entity);
                    }
#endif
                }
            }

#if EXCEPTION_TOLERANCE
            foreach (var entity in brokenEnts)
            {
                _entityManager.DeleteEntity(entity);
            }
#endif
        }

        private void HandleEntityState(EntityUid uid, IEventBus bus, EntityState? curState,
            EntityState? nextState, GameTick lastApplied, GameTick toTick, bool enteringPvs)
        {
            var size = curState?.ComponentChanges.Span.Length ?? 0 + nextState?.ComponentChanges.Span.Length ?? 0;
            var compStateWork = new Dictionary<ushort, (IComponent Component, ComponentState? curState, ComponentState? nextState)>(size);

            if (enteringPvs)
            {
                // last-server state has already been updated with new information from curState
                // --> simply reset to the most recent server state.
                //
                // as to why we need to reset: because in the process of detaching to null-space, we will have dirtied
                // the entity. most notably, all entities will have been ejected from their containers.
                foreach (var (id, state) in _processor.GetLastServerStates(uid))
                {
                    if (!_entityManager.TryGetComponent(uid, id, out var comp))
                    {
                        comp = _compFactory.GetComponent(id);
                        var newComp = (Component)comp;
                        newComp.Owner = uid;
                        _entityManager.AddComponent(uid, newComp, true);
                    }

                    compStateWork[id] = (comp, state, null);
                }
            }
            else if (curState != null)
            {
                foreach (var compChange in curState.ComponentChanges.Span)
                {
                    if (compChange.Deleted)
                    {
                        _entityManager.RemoveComponent(uid, compChange.NetID);
                        continue;
                    }

                    if (!_entityManager.TryGetComponent(uid, compChange.NetID, out var comp))
                    {
                        comp = _compFactory.GetComponent(compChange.NetID);
                        var newComp = (Component)comp;
                        newComp.Owner = uid;
                        _entityManager.AddComponent(uid, newComp, true);
                    }
                    else if (compChange.LastModifiedTick <= lastApplied && lastApplied != GameTick.Zero)
                        continue;

                    compStateWork[compChange.NetID] = (comp, compChange.State, null);
                }
            }

            if (nextState != null)
            {
                foreach (var compState in nextState.ComponentChanges.Span)
                {
                    if (compState.LastModifiedTick != toTick)
                        continue;

                    if (!_entityManager.TryGetComponent(uid, compState.NetID, out var comp))
                    {
                        // The component can be null here due to interp, because the NEXT state will have a new
                        // component, but the component does not yet exist.
                        continue;
                    }

                    if (compStateWork.TryGetValue(compState.NetID, out var state))
                        compStateWork[compState.NetID] = (comp, state.curState, compState.State);
                    else
                        compStateWork[compState.NetID] = (comp, null, compState.State);
                }
            }

            foreach (var (comp, cur, next) in compStateWork.Values)
            {
                try
                {
                    var handleState = new ComponentHandleState(cur, next);
                    bus.RaiseComponentEvent(comp, ref handleState);
                    comp.HandleComponentState(cur, next);
                }
                catch (Exception e)
                {
                    var wrapper = new ComponentStateApplyException(
                        $"Failed to apply comp state: entity={comp.Owner}, comp={comp.GetType()}", e);
#if EXCEPTION_TOLERANCE
                _runtimeLog.LogException(wrapper, "Component state apply");
#else
                    throw wrapper;
#endif
                }
            }
        }
    }

    public sealed class GameStateAppliedArgs : EventArgs
    {
        public GameState AppliedState { get; }
        public readonly List<EntityUid> Detached;

        public GameStateAppliedArgs(GameState appliedState, List<EntityUid> detached)
        {
            AppliedState = appliedState;
            Detached = detached;
        }
    }
}
