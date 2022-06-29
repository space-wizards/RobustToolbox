// ReSharper disable once RedundantUsingDirective
// Used in EXCEPTION_TOLERANCE preprocessor
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.Timing;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Players;
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
        private int _pvsDetatchBudget;

        /// <inheritdoc />
        public event Action<GameStateAppliedArgs>? GameStateApplied;

        /// <inheritdoc />
        public void Initialize()
        {
            _sawmill = Logger.GetSawmill(CVars.NetPredict.Name);
            _processor = new GameStateProcessor(_timing);

            _network.RegisterNetMessage<MsgState>(HandleStateMessage);
            _network.RegisterNetMessage<MsgStateLeavePvs>(HandlePvsLeaveMessage);
            _network.RegisterNetMessage<MsgStateAck>();
            _client.RunLevelChanged += RunLevelChanged;

            _config.OnValueChanged(CVars.NetInterp, b => _processor.Interpolation = b, true);
            _config.OnValueChanged(CVars.NetInterpRatio, i => _processor.InterpRatio = i, true);
            _config.OnValueChanged(CVars.NetLogging, b => _processor.Logging = b, true);
            _config.OnValueChanged(CVars.NetPredict, b => IsPredictionEnabled = b, true);
            _config.OnValueChanged(CVars.NetPredictTickBias, i => PredictTickBias = i, true);
            _config.OnValueChanged(CVars.NetPredictLagBias, i => PredictLagBias = i, true);
            _config.OnValueChanged(CVars.NetStateBufMergeThreshold, i => StateBufferMergeThreshold = i, true);
            _config.OnValueChanged(CVars.NetPVSEntityExitBudget, i => _pvsDetatchBudget = i, true);

            _processor.Interpolation = _config.GetCVar(CVars.NetInterp);
            _processor.InterpRatio = _config.GetCVar(CVars.NetInterpRatio);
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
            var state = message.State;

            _processor.AddNewState(state);

            // we always ack everything we receive, even if it is late
            AckGameState(state.ToSequence);
        }

        private void HandlePvsLeaveMessage(MsgStateLeavePvs message)
        {
            _processor.AddLeavePvsMessage(message);
        }

        /// <inheritdoc />
        public void ApplyGameState()
        {
            // Calculate how many states we need to apply this tick.
            // Always at least one, but can be more based on StateBufferMergeThreshold.
            var curBufSize = CurrentBufferSize;
            var targetBufSize = TargetBufferSize;
            var applyCount = Math.Max(1 + _timing.LastProcessedTick.Value - _timing.LastRealTick.Value, curBufSize - targetBufSize - StateBufferMergeThreshold);

            _prof.WriteValue($"State buffer size", curBufSize);
            _prof.WriteValue($"State apply count", applyCount);

            _timing.LastProcessedTick = _timing.LastRealTick;

            var i = 0;
            for (; i < applyCount; i++)
            {
                // Attempts to retrieve the next state to apply
                // TODO: We could theoretically communicate with the GameStateProcessor better here.
                // Since game states are sliding windows, it is possible that we need less than applyCount applies here.
                // Consider, if you have 3 states, (tFrom=1, tTo=2), (tFrom=1, tTo=3), (tFrom=2, tTo=3),
                // you only need to apply the last 2 states to go from 1 -> 3.
                // instead of all 3.
                // This would be a nice optimization though also minor since the primary cost here
                // is avoiding entity system and re-prediction runs.
                if (!_processor.TryGetNextStates(out var curState, out var nextState))
                    break;

                _processor.WaitingForFull = false;

                if (PredictionNeedsResetting)
                    ResetPredictedEntities();

                // Increase last processed tick.
                // This is usually functionally just a +=1, except when applying a full state.
                _timing.LastProcessedTick = curState.ToSequence;

                if (curState.Extrapolated)
                    continue; // This is not a real state, we will not actually apply it.

                _timing.LastRealTick = _timing.LastProcessedTick;

                using (_prof.Group("FullRep"))
                {
                    _processor.UpdateFullRep(curState);
                }

                IEnumerable<EntityUid> createdEntities;
                using (_prof.Group("ApplyGameState"))
                {
                    if (i + 1 < applyCount && nextState != null && !nextState.Extrapolated)
                    {
                        // We are about to apply another state after this one anyways. So there is no need to pass in
                        // the next state for frame interpolation. Really the !nextState.Extrapolated should be replaced
                        // by a (!nextState.Extrapolated || (i+2 < applycount && !next_next_state.Extrapolated)), and so
                        // on, but this is good enough.
                        //
                        // Basically: we only need to apply next-state for the last cur-state we are applying. but 99%
                        // of the time, we are only applying a single tick. But if we are applying more than one the
                        // client tends to stutter, so this sort of matters.
                        nextState = null;
                    }

                    createdEntities = ApplyGameState(curState, nextState);
                }

                using (_prof.Group("MergeImplicitData"))
                {
                    MergeImplicitData(createdEntities);
                }

                if (_lastProcessedInput < curState.LastProcessedInput)
                {
                    _sawmill.Debug($"SV> RCV  tick={_timing.CurTick}, seq={_lastProcessedInput}");
                    _lastProcessedInput = curState.LastProcessedInput;
                }
            }

            _timing.CurTick = _timing.LastRealTick;

            // Slightly speed up or slow down the client tickrate based on the contents of the buffer.
            // TryGetTickStates should have cleaned out any old states, so the buffer contains [t-1(last), t+0(cur), t+1(next), t+2, t+3, ..., t+n]
            // we can use this info to properly time our tickrate so it does not run fast or slow compared to the server.
            if (_processor.WaitingForFull)
                _timing.TickTimingAdjustment = 0f;
            else
                _timing.TickTimingAdjustment = (CurrentBufferSize - (float)TargetBufferSize) * 0.10f;

            if (i == 0)
            {
                // Didn't apply a single state successfully.
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

            if (IsPredictionEnabled)
                PredictTicks();

            using (_prof.Group("Tick"))
            {
                _entities.TickUpdate((float) _timing.TickPeriod.TotalSeconds, noPredictions: !IsPredictionEnabled);
            }
        }

        public void PredictTicks()
        {
            var ping = (_network.ServerChannel?.Ping ?? 0) / 1000f + PredictLagBias; // seconds.
            var predictionTarget = _timing.CurTick.Value + _processor.TargetBufferSize +
                             (int)Math.Ceiling(_timing.TickRate * ping) + PredictTickBias;

            // If we are about to process an another tick in the same frame, lets not bother unnecessarily running prediction ticks
            // Really the main-loop ticking just needs to be more specialized for clients.
            var skipPrediction = _timing.TickRemainder >= _timing.CalcAdjustedTickPeriod();
            if (skipPrediction)
            {
                _timing.CurTick = new((uint) predictionTarget);
                return;
            }

            PredictionNeedsResetting = true;
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

            for (var t = _timing.CurTick.Value + 1; t <= predictionTarget; t++)
            {
                var groupStart = _prof.WriteGroupStart();

                var tick = new GameTick(t);
                _timing.CurTick = tick;

                while (hasPendingInput && pendingInputEnumerator.Current.Tick <= tick)
                {
                    var inputCmd = pendingInputEnumerator.Current;

                    _inputManager.NetworkBindMap.TryGetKeyFunction(inputCmd.InputFunctionId, out var boundFunc);

                    _sawmill.Debug(
                        $"    seq={inputCmd.InputSequence}, sub={inputCmd.SubTick}, dTick={tick}, func={boundFunc.FunctionName}, " +
                        $"state={inputCmd.State}");


                    input.PredictInputCommand(inputCmd);

                    hasPendingInput = pendingInputEnumerator.MoveNext();
                }

                while (hasPendingMessage && pendingMessagesEnumerator.Current.sourceTick <= tick)
                {
                    var msg = pendingMessagesEnumerator.Current.msg;

                    _entities.EventBus.RaiseEvent(EventSource.Local, msg);
                    _entities.EventBus.RaiseEvent(EventSource.Local, pendingMessagesEnumerator.Current.sessionMsg);

                    hasPendingMessage = pendingMessagesEnumerator.MoveNext();
                }

                if (t != predictionTarget)
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

                _prof.WriteGroupEnd(groupStart, "Prediction tick", ProfData.Int64(t));
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
        ///     initial server state for any newly created entity. It does this byu simply uisng the standard <see
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

            (IEnumerable<EntityUid> Created, List<EntityUid> Detatched) output;
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
                GameStateApplied?.Invoke(new GameStateAppliedArgs(curState, output.Detatched));
            }

            return output.Created;
        }

        private (IEnumerable<EntityUid> Created, List<EntityUid> Detatched) ApplyEntityStates(GameState curState, GameState? nextState)
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

                bool isEnteringPvs = (meta.Flags & MetaDataFlags.Detatched) != 0;
                if (isEnteringPvs)
                {
                    meta.Flags &= ~MetaDataFlags.Detatched;
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
            var detatched = ProcessPvsDeparture(curState.ToSequence, metas, xforms);

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

            // Delete Entities. This HAS to happen after apply entity state, because some entities may have been
            // de-parented from an entity that is about to be deleted.
            var delSpan = curState.EntityDeletions.Span;
            if (delSpan.Length > 0)
            {
                using var _ = _prof.Group("Deletion");
                foreach (var id in delSpan)
                {
                    _entities.DeleteEntity(id);
                }
                _prof.WriteValue("Count", ProfData.Int32(delSpan.Length));
            }

            // Initialize and start the newly created entities.
            // IMO this should be done before apply-state, so that people can safely make use of things like
            // on-container-insert events without having to check whether entities have been initialized yet. but this
            // seems to cause some weird issues for transform components when they get initialized before a state has
            // been applied?? It would also avoid situations were a system accidentally dirties a component during
            // init/startup, because these results are going to be saved via merge implicit data, and we really want
            // that to match the server state
            if (toCreate.Count > 0)
                InitializeAndStart(toCreate);

            _prof.WriteValue("State Size", ProfData.Int32(curSpan.Length));
            _prof.WriteValue("Entered PVS", ProfData.Int32(enteringPvs));

            return (toCreate.Keys, detatched);
        }

        private List<EntityUid> ProcessPvsDeparture(GameTick toTick, EntityQuery<MetaDataComponent> metas, EntityQuery<TransformComponent> xforms)
        {
            var toDetatch = _processor.GetEntitiesToDetatch(toTick, _pvsDetatchBudget);
            var detatched = new List<EntityUid>();

            if (toDetatch.Count == 0)
                return detatched;

            // TODO optimize
            // If an entity is leaving PVS, so are all of its children. If we can preserve the hierarchy we can avoid
            // things like container insertion and ejection.

            using var _ = _prof.Group("Leave PVS");

            var xformSys = _entitySystemManager.GetEntitySystem<SharedTransformSystem>();

            foreach (var (tick, ents) in toDetatch)
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

                    if ((meta.Flags & MetaDataFlags.Detatched) != 0)
                        continue;

                    meta.Flags |= MetaDataFlags.Detatched;
                    meta.LastStateApplied = toTick;

                    var xform = xforms.GetComponent(ent);
                    if (xform.ParentUid.IsValid())
                        xformSys.DetachParentToNull(xform, xforms, metas);
                    detatched.Add(ent);
                }
            }

            _prof.WriteValue("Count", ProfData.Int32(detatched.Count));
            return detatched;
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
                var serverState = _processor.GetLastServerStates(uid);
                foreach (var (id, comp) in _entities.GetNetComponents(uid))
                {
                    if (comp.NetSyncEnabled && !serverState.ContainsKey(id))
                        _entityManager.RemoveComponent(uid, comp);

                }

                foreach (var (id, state) in serverState)
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
        public readonly List<EntityUid> Detatched;

        public GameStateAppliedArgs(GameState appliedState, List<EntityUid> detatched)
        {
            AppliedState = appliedState;
            Detatched = detatched;
        }
    }
}
