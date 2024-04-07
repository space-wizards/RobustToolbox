// ReSharper disable once RedundantUsingDirective
// Used in EXCEPTION_TOLERANCE preprocessor
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Microsoft.Extensions.ObjectPool;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Client.Timing;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Profiling;
using Robust.Shared.Replays;
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

        // Game state dictionaries that get used every tick.
        private readonly Dictionary<EntityUid, (NetEntity NetEntity, MetaDataComponent Meta, bool EnteringPvs, GameTick LastApplied, EntityState? curState, EntityState? nextState)> _toApply = new();
        private readonly Dictionary<NetEntity, EntityState> _toCreate = new();
        private readonly Dictionary<ushort, (IComponent Component, IComponentState? curState, IComponentState? nextState)> _compStateWork = new();
        private readonly Dictionary<EntityUid, HashSet<Type>> _pendingReapplyNetStates = new();
        private readonly HashSet<NetEntity> _stateEnts = new();
        private readonly List<EntityUid> _toDelete = new();
        private readonly List<IComponent> _toRemove = new();
        private readonly Dictionary<NetEntity, Dictionary<ushort, IComponentState>> _outputData = new();
        private readonly List<(EntityUid, TransformComponent)> _queuedBroadphaseUpdates = new();

        private readonly ObjectPool<Dictionary<ushort, IComponentState>> _compDataPool =
            new DefaultObjectPool<Dictionary<ushort, IComponentState>>(new DictPolicy<ushort, IComponentState>(), 256);

        private uint _metaCompNetId;

        [Dependency] private readonly IReplayRecordingManager _replayRecording = default!;
        [Dependency] private readonly IComponentFactory _compFactory = default!;
        [Dependency] private readonly IClientEntityManagerInternal _entities = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IClientNetManager _network = default!;
        [Dependency] private readonly IBaseClient _client = default!;
        [Dependency] private readonly IClientGameTiming _timing = default!;
        [Dependency] private readonly INetConfigurationManager _config = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IConsoleHost _conHost = default!;
        [Dependency] private readonly ClientEntityManager _entityManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly ProfManager _prof = default!;
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
        [Dependency] private readonly ILogManager _logMan = default!;

        private ISawmill _sawmill = default!;

        /// <summary>
        /// If we are waiting for a full game state from the server, we will automatically re-send full state requests
        /// if they do not arrive in time. Ideally this should never happen, this here just in case a client gets
        /// stuck waiting for a full state that the server doesn't know the client even wants.
        /// </summary>
        public static readonly TimeSpan FullStateTimeout = TimeSpan.FromSeconds(10);

        /// <inheritdoc />
        public int MinBufferSize => _processor.MinBufferSize;

        /// <inheritdoc />
        public int TargetBufferSize => _processor.TargetBufferSize;

        /// <inheritdoc />
        public int GetApplicableStateCount() => _processor.GetApplicableStateCount();
        public int StateCount => _processor.StateCount;

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

#if DEBUG
        /// <summary>
        /// If true, this will cause received game states to be ignored. Used by integration tests.
        /// </summary>
        public bool DropStates;
#endif

        private bool _resettingPredictedEntities;

        /// <inheritdoc />
        public void Initialize()
        {
            _sawmill = _logMan.GetSawmill("state");
            _sawmill.Level = LogLevel.Info;

            _processor = new GameStateProcessor(this, _timing, _sawmill);

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
            _config.OnValueChanged(CVars.NetMaxBufferSize, i => _processor.MaxBufferSize = i, true);

            _processor.Interpolation = _config.GetCVar(CVars.NetInterp);
            _processor.BufferSize = _config.GetCVar(CVars.NetBufferSize);
            _processor.Logging = _config.GetCVar(CVars.NetLogging);
            IsPredictionEnabled = _config.GetCVar(CVars.NetPredict);
            PredictTickBias = _config.GetCVar(CVars.NetPredictTickBias);
            PredictLagBias = _config.GetCVar(CVars.NetPredictLagBias);

            _conHost.RegisterCommand("resetent", Loc.GetString("cmd-reset-ent-desc"), Loc.GetString("cmd-reset-ent-help"), ResetEntCommand);
            _conHost.RegisterCommand("resetallents", Loc.GetString("cmd-reset-all-ents-desc"), Loc.GetString("cmd-reset-all-ents-help"), ResetAllEnts);
            _conHost.RegisterCommand("detachent", Loc.GetString("cmd-detach-ent-desc"), Loc.GetString("cmd-detach-ent-help"), DetachEntCommand);
            _conHost.RegisterCommand("localdelete", Loc.GetString("cmd-local-delete-desc"), Loc.GetString("cmd-local-delete-help"), LocalDeleteEntCommand);
            _conHost.RegisterCommand("fullstatereset", Loc.GetString("cmd-full-state-reset-desc"), Loc.GetString("cmd-full-state-reset-help"), (_,_,_) => RequestFullState());

            _entities.ComponentAdded += OnComponentAdded;

            var metaId = _compFactory.GetRegistration(typeof(MetaDataComponent)).NetID;
            if (!metaId.HasValue)
                throw new InvalidOperationException("MetaDataComponent does not have a NetId.");

            _metaCompNetId = metaId.Value;
        }

        private void OnComponentAdded(AddedComponentEventArgs args)
        {
            if (!_resettingPredictedEntities)
                return;

            var comp = args.ComponentType;
            if (comp.NetID == null)
                return;

            if (_entityManager.IsClientSide(args.BaseArgs.Owner))
                return;

            _sawmill.Error($"""
                Added component {comp.Name} to entity {_entityManager.ToPrettyString(args.BaseArgs.Owner)} while resetting predicted entities.
                Stack trace:
                {Environment.StackTrace}
                """);
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

        public void InputCommandDispatched(ClientFullInputCmdMessage clientMessage, FullInputCmdMessage message)
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

            DebugTools.Assert(_players.LocalSession != null);

            var evArgs = new EntitySessionEventArgs(_players.LocalSession);
            _pendingSystemMessages.Enqueue((_nextInputCmdSeq, _timing.CurTick, message,
                new EntitySessionMessage<T>(evArgs, message)));

            return _nextInputCmdSeq++;
        }

        private void HandleStateMessage(MsgState message)
        {
#if DEBUG
            if (DropStates)
                return;
#endif
            // We ONLY ack states that are definitely going to get applied. Otherwise the sever might assume that we
            // applied a state containing entity-creation information, which it would then no longer send to us when
            // we re-encounter this entity
            if (_processor.AddNewState(message.State))
                AckGameState(message.State.ToSequence);
        }

        public void UpdateFullRep(GameState state, bool cloneDelta = false)
            => _processor.UpdateFullRep(state, cloneDelta);

        public Dictionary<NetEntity, Dictionary<ushort, IComponentState>> GetFullRep()
            => _processor.GetFullRep();

        private void HandlePvsLeaveMessage(MsgStateLeavePvs message)
        {
            QueuePvsDetach(message.Entities, message.Tick);
            PvsLeave?.Invoke(message);
        }

        public void QueuePvsDetach(List<NetEntity> entities, GameTick tick)
        {
            _processor.AddLeavePvsMessage(entities, tick);
            if (_replayRecording.IsRecording)
                _replayRecording.RecordClientMessage(new ReplayMessage.LeavePvs(entities, tick));
        }

        public void ClearDetachQueue() => _processor.ClearDetachQueue();

        /// <inheritdoc />
        public void ApplyGameState()
        {
            // If we have been waiting for a full state for a long time, re-request a full state.
            if (_processor.WaitingForFull
                && _processor.LastFullStateRequested is {} last
                && DateTime.UtcNow - last.Time > FullStateTimeout)
            {
                // Re-request a full state.
                // We use the previous from-tick, just in case the full state is already on the way,
                RequestFullState(null, last.Tick);
            }

            // Calculate how many states we need to apply this tick.
            // Always at least one, but can be more based on StateBufferMergeThreshold.
            var curBufSize = GetApplicableStateCount();
            var targetBufSize = TargetBufferSize;

            var bufferOverflow = curBufSize - targetBufSize - StateBufferMergeThreshold;
            var targetProcessedTick = (bufferOverflow > 1)
                ? _timing.LastProcessedTick + (uint)bufferOverflow
                : _timing.LastProcessedTick + 1;

            _prof.WriteValue($"State buffer size", curBufSize);
            _prof.WriteValue($"State apply count", targetProcessedTick.Value - _timing.LastProcessedTick.Value);

            bool processedAny = false;

            _timing.LastProcessedTick = _timing.LastRealTick;
            while (_timing.LastProcessedTick < targetProcessedTick)
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
                    break;

                processedAny = true;

                if (curState == null)
                {
                    // Might just be missing a state, but we may be able to make use of a future state if it has a low enough from sequence.
                    _timing.LastProcessedTick += 1;
                    continue;
                }

                try
                {
                    ResetPredictedEntities();
                }
                catch (Exception e)
                {
                    // avoid exception spam from repeatedly trying to reset the same entity.
                    _entitySystemManager.GetEntitySystem<ClientDirtySystem>().Reset();
                    _runtimeLog.LogException(e, "ResetPredictedEntities");
                }

                // If we were waiting for a new state, we are now applying it.
                if (curState.FromSequence == GameTick.Zero)
                {
                    _processor.OnFullStateReceived();
                    _timing.LastProcessedTick = curState.ToSequence;
                    DebugTools.Assert(curState.FromSequence == GameTick.Zero);
                    PartialStateReset(curState, true);
                }
                else
                {
                    DebugTools.Assert(!_processor.WaitingForFull);
                    _timing.LastProcessedTick += 1;
                }

                _timing.CurTick = _timing.LastRealTick = _timing.LastProcessedTick;

                // Update the cached server state.
                using (_prof.Group("FullRep"))
                {
                    _processor.UpdateFullRep(curState);
                }

                IEnumerable<NetEntity> createdEntities;
                using (_prof.Group("ApplyGameState"))
                {
                    if (_timing.LastProcessedTick < targetProcessedTick && nextState != null)
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
                    catch (MissingMetadataException e)
                    {
                        // Something has gone wrong. Probably a missing meta-data component. Perhaps a full server state will fix it.
                        RequestFullState(e.NetEntity);
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
                _timing.TickTimingAdjustment = (GetApplicableStateCount() - (float)TargetBufferSize) * 0.10f;

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

        public void RequestFullState(NetEntity? missingEntity = null, GameTick? tick = null)
        {
            _sawmill.Info("Requesting full server state");
            _network.ClientSendMessage(new MsgStateRequestFull { Tick = _timing.LastRealTick , MissingEntity = missingEntity ?? NetEntity.Invalid });
            _processor.OnFullStateRequested(tick ?? _timing.LastRealTick);
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
            using var pendingInputEnumerator = _pendingInputs.GetEnumerator();
            using var pendingMessagesEnumerator = _pendingSystemMessages.GetEnumerator();
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

        public void ResetPredictedEntities()
        {
            using var _ = _prof.Group("ResetPredictedEntities");
            using var __ = _timing.StartStateApplicationArea();

            // This is terrible, and I hate it. This also needs to run even when prediction is disabled.
            _entitySystemManager.GetEntitySystem<TransformSystem>().Reset();

            if (!PredictionNeedsResetting)
                return;

            PredictionNeedsResetting = false;
            var countReset = 0;
            var system = _entitySystemManager.GetEntitySystem<ClientDirtySystem>();
            var metaQuery = _entityManager.GetEntityQuery<MetaDataComponent>();
            RemQueue<IComponent> toRemove = new();

            foreach (var entity in system.DirtyEntities)
            {
                DebugTools.Assert(toRemove.Count == 0);
                // Check log level first to avoid the string alloc.
                if (_sawmill.Level <= LogLevel.Debug)
                    _sawmill.Debug($"Entity {entity} was made dirty.");

                if (!metaQuery.TryGetComponent(entity, out var meta) ||
                    !_processor.TryGetLastServerStates(meta.NetEntity, out var last))
                {
                    // Entity was probably deleted on the server so do nothing.
                    continue;
                }

                countReset += 1;

                try
                {
                    _resettingPredictedEntities = true;

                    foreach (var (netId, comp) in meta.NetComponents)
                    {
                        if (!comp.NetSyncEnabled)
                            continue;

                        // Was this component added during prediction?
                        if (comp.CreationTick > _timing.LastRealTick)
                        {
                            if (last.ContainsKey(netId))
                            {
                                // Component was probably removed and then re-addedd during a single prediction run
                                // Just reset state as normal.
                                comp.ClearCreationTick();
                            }
                            else
                            {
                                toRemove.Add(comp);
                                if (_sawmill.Level <= LogLevel.Debug)
                                    _sawmill.Debug($"  A new component was added: {comp.GetType()}");
                                continue;
                            }
                        }

                        if (comp.LastModifiedTick <= _timing.LastRealTick ||
                            !last.TryGetValue(netId, out var compState))
                        {
                            continue;
                        }

                        if (_sawmill.Level <= LogLevel.Debug)
                            _sawmill.Debug($"  A component was dirtied: {comp.GetType()}");

                        var handleState = new ComponentHandleState(compState, null);
                        _entities.EventBus.RaiseComponentEvent(comp, ref handleState);
                        comp.LastModifiedTick = _timing.LastRealTick;
                    }
                }
                finally
                {
                    _resettingPredictedEntities = false;
                }

                // Remove predicted component additions
                foreach (var comp in toRemove)
                {
                    _entities.RemoveComponent(entity, comp);
                }
                toRemove.Clear();

                // Re-add predicted removals
                if (system.RemovedComponents.TryGetValue(entity, out var netIds))
                {
                    foreach (var netId in netIds)
                    {
                        if (meta.NetComponents.ContainsKey(netId))
                            continue;

                        if (!last.TryGetValue(netId, out var state))
                            continue;

                        var comp = _entityManager.AddComponent(entity, netId, meta);

                        if (_sawmill.Level <= LogLevel.Debug)
                            _sawmill.Debug($"  A component was removed: {comp.GetType()}");

                        var stateEv = new ComponentHandleState(state, null);
                        _entities.EventBus.RaiseComponentEvent(comp, ref stateEv);
                        comp.ClearCreationTick(); // don't undo the re-adding.
                        comp.LastModifiedTick = _timing.LastRealTick;
                    }
                }

                DebugTools.Assert(meta.EntityLastModifiedTick > _timing.LastRealTick);
                meta.EntityLastModifiedTick = _timing.LastRealTick;
            }

            _entityManager.System<PhysicsSystem>().ResetContacts();

            // TODO maybe reset more of physics?
            // E.g., warm impulses for warm starting?

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
        ///     cref="IEntityManager.GetComponentState"/>.
        /// </remarks>
        private void MergeImplicitData(IEnumerable<NetEntity> createdEntities)
        {
            var bus = _entityManager.EventBus;

            foreach (var netEntity in createdEntities)
            {
                var (_, meta) = _entityManager.GetEntityData(netEntity);
                var compData = _compDataPool.Get();
                _outputData.Add(netEntity, compData);

                foreach (var (netId, component) in meta.NetComponents)
                {
                    DebugTools.Assert(component.NetSyncEnabled);

                    var state = _entityManager.GetComponentState(bus, component, null, GameTick.Zero);
                    DebugTools.Assert(state is not IComponentDeltaState delta || delta.FullState);
                    compData.Add(netId, state);
                }
            }

            _processor.MergeImplicitData(_outputData);

            foreach (var data in _outputData.Values)
            {
                _compDataPool.Return(data);
            }

            _outputData.Clear();
        }

        private void AckGameState(GameTick sequence)
        {
            _network.ClientSendMessage(new MsgStateAck() { Sequence = sequence });
        }

        public IEnumerable<NetEntity> ApplyGameState(GameState curState, GameState? nextState)
        {
            using var _ = _timing.StartStateApplicationArea();

            // TODO replays optimize this.
            // This currently just saves game states as they are applied.
            // However this is inefficient and may have redundant data.
            // E.g., we may record states: [10 to 15] [11 to 16] *error* [0 to 18] [18 to 19] [18 to 20] ...
            // The best way to deal with this is probably to just re-process & re-write the replay when first loading it.
            //
            // Also, currently this will cause a state to be serialized, which in principle shouldn't differ from the
            // data that we received from the server. So if a recording is active we could actually just copy those
            // raw bytes.
            _replayRecording.Update(curState);

            using (_prof.Group("Config"))
            {
                _config.TickProcessMessages();
            }

            (IEnumerable<NetEntity> Created, List<NetEntity> Detached) output;
            using (_prof.Group("Entity"))
            {
                output = ApplyEntityStates(curState, nextState);
            }

            using (_prof.Group("Player"))
            {
                _players.ApplyPlayerStates(curState.PlayerStates.Value ?? Array.Empty<SessionState>());
            }

            using (_prof.Group("Callback"))
            {
                GameStateApplied?.Invoke(new GameStateAppliedArgs(curState, output.Detached));
            }

            return output.Created;
        }

        private (IEnumerable<NetEntity> Created, List<NetEntity> Detached) ApplyEntityStates(GameState curState, GameState? nextState)
        {
            var metas = _entities.GetEntityQuery<MetaDataComponent>();
            var xforms = _entities.GetEntityQuery<TransformComponent>();
            var xformSys = _entitySystemManager.GetEntitySystem<SharedTransformSystem>();

            var enteringPvs = 0;
            _toApply.Clear();
            _toCreate.Clear();
            _pendingReapplyNetStates.Clear();
            var curSpan = curState.EntityStates.Span;

            // Create new entities
            // This is done BEFORE state application to ensure any new parents exist before existing children have their states applied, otherwise, we may have issues with entity transforms!
            {
                using var _ = _prof.Group("Create uninitialized entities");
                var count = 0;

                foreach (var es in curSpan)
                {
                    if (_entityManager.TryGetEntity(es.NetEntity, out var nUid))
                    {
                        DebugTools.Assert(_entityManager.EntityExists(nUid));
                        continue;
                    }

                    count++;
                    var metaState = (MetaDataComponentState?)es.ComponentChanges.Value?.FirstOrDefault(c => c.NetID == _metaCompNetId).State;
                    if (metaState == null)
                        throw new MissingMetadataException(es.NetEntity);

                    var uid = _entities.CreateEntity(metaState.PrototypeId, out var newMeta);
                    _toCreate.Add(es.NetEntity, es);
                    _toApply.Add(uid, (es.NetEntity, newMeta, false, GameTick.Zero, es, null));

                    // Client creates a client-side net entity for the newly created entity.
                    // We need to clear this mapping before assigning the real net id.
                    // TODO NetEntity Jank: prevent the client from creating this in the first place.
                    _entityManager.ClearNetEntity(newMeta.NetEntity);

                    _entityManager.SetNetEntity(uid, es.NetEntity, newMeta);
                    newMeta.LastStateApplied = curState.ToSequence;

                    // Check if there's any component states awaiting this entity.
                    if (_entityManager.PendingNetEntityStates.Remove(es.NetEntity, out var value))
                    {
                        foreach (var (type, owner) in value)
                        {
                            var pending = _pendingReapplyNetStates.GetOrNew(owner);
                            pending.Add(type);
                        }
                    }
                }

                _prof.WriteValue("Count", ProfData.Int32(count));
            }

            foreach (var es in curSpan)
            {
                if (_toCreate.ContainsKey(es.NetEntity))
                    continue;

                if (!_entityManager.TryGetEntityData(es.NetEntity, out var uid, out var meta))
                    continue;

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

                _toApply.Add(uid.Value, (es.NetEntity, meta, isEnteringPvs, meta.LastStateApplied, es, null));
                meta.LastStateApplied = curState.ToSequence;
            }

            // Detach entities to null space
            var containerSys = _entitySystemManager.GetEntitySystem<ContainerSystem>();
            var lookupSys = _entitySystemManager.GetEntitySystem<EntityLookupSystem>();
            var detached = ProcessPvsDeparture(curState.ToSequence, metas, xforms, xformSys, containerSys, lookupSys);

            // Check next state (AFTER having created new entities introduced in curstate)
            if (nextState != null)
            {
                foreach (var es in nextState.EntityStates.Span)
                {
                    if (!_entityManager.TryGetEntityData(es.NetEntity, out var uid, out var meta))
                        continue;

                    // Does the next state actually have any future information about this entity that could be used for interpolation?
                    if (es.EntityLastModified != nextState.ToSequence)
                        continue;

                    ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(_toApply, uid.Value, out var exists);

                    if (exists)
                        state = (es.NetEntity, meta, state.EnteringPvs, state.LastApplied, state.curState, es);
                    else
                        state = (es.NetEntity, meta, false, GameTick.Zero, null, es);
                }
            }

            // Check pending states and see if we need to force any entities to re-run component states.
            foreach (var uid in _pendingReapplyNetStates.Keys)
            {
                // Original entity referencing the NetEntity may have been deleted.
                if (!metas.TryGetComponent(uid, out var meta))
                    continue;

                // It may also have been queued for deletion, in which case its last server state entry has already been removed.
                // I love me some spaghetti order-of-operation dependent code

                if (!_processor._lastStateFullRep.ContainsKey(meta.NetEntity))
                {
                    DebugTools.Assert(curState.EntityDeletions.Value.Contains(meta.NetEntity));
                    continue;
                }

                DebugTools.Assert(!curState.EntityDeletions.Value.Contains(meta.NetEntity));

                // State already being re-applied so don't bulldoze it.
                ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(_toApply, uid, out var exists);

                if (exists)
                    continue;

                state = (meta.NetEntity, meta, false, GameTick.Zero, null, null);
            }

            _queuedBroadphaseUpdates.Clear();

            // Apply entity states.
            using (_prof.Group("Apply States"))
            {
                foreach (var (entity, data) in _toApply)
                {
                    HandleEntityState(entity, data.NetEntity, data.Meta, _entities.EventBus, data.curState,
                        data.nextState, data.LastApplied, curState.ToSequence, data.EnteringPvs);

                    if (!data.EnteringPvs)
                        continue;

                    // Now that things like collision data, fixtures, and positions have been updated, we queue a
                    // broadphase update. However, if this entity is parented to some other entity also re-entering PVS,
                    // we only need to update it's parent (as it recursively updates children anyways).
                    var xform = xforms.GetComponent(entity);
                    DebugTools.Assert(xform.Broadphase == BroadphaseData.Invalid);
                    xform.Broadphase = null;
                    if (!_toApply.TryGetValue(xform.ParentUid, out var parent) || !parent.EnteringPvs)
                        _queuedBroadphaseUpdates.Add((entity, xform));
                }

                _prof.WriteValue("Count", ProfData.Int32(_toApply.Count));
            }

            // Add entering entities back to broadphase.
            using (_prof.Group("Update Broadphase"))
            {
                try
                {
                    foreach (var (uid, xform) in _queuedBroadphaseUpdates)
                    {
                        lookupSys.FindAndAddToEntityTree(uid, true, xform);
                    }
                }
                catch (Exception e)
                {
                    _sawmill.Error($"Caught exception while updating entity broadphases");
                    _runtimeLog.LogException(e, $"{nameof(ClientGameStateManager)}.{nameof(ApplyEntityStates)}");
                }
            }

            var delSpan = curState.EntityDeletions.Span;
            if (delSpan.Length > 0)
            {
                try
                {
                    ProcessDeletions(delSpan, xforms, xformSys);
                }
                catch (Exception e)
                {
                    _sawmill.Error($"Caught exception while deleting entities");
                    _runtimeLog.LogException(e, $"{nameof(ClientGameStateManager)}.{nameof(ApplyEntityStates)}");
                }
            }

            // Initialize and start the newly created entities.
            if (_toCreate.Count > 0)
                InitializeAndStart(_toCreate);

            _prof.WriteValue("State Size", ProfData.Int32(curSpan.Length));
            _prof.WriteValue("Entered PVS", ProfData.Int32(enteringPvs));

            return (_toCreate.Keys, detached);
        }

        /// <inheritdoc />
        public void PartialStateReset(
            GameState state,
            bool resetAllEntities,
            bool deleteClientEntities = false,
            bool deleteClientChildren = true)
        {
            using var _ = _timing.StartStateApplicationArea();

            if (state.FromSequence != GameTick.Zero)
            {
                _sawmill.Error("Attempted to reset to a state with incomplete data");
                return;
            }

            _sawmill.Info($"Resetting all entity states to tick {state.ToSequence}.");

            // Construct hashset for set.Contains() checks.
            _stateEnts.Clear();
            var entityStates = state.EntityStates.Span;
            foreach (var entState in entityStates)
            {
                _stateEnts.Add(entState.NetEntity);
            }

            var xforms = _entities.GetEntityQuery<TransformComponent>();
            var xformSys = _entitySystemManager.GetEntitySystem<SharedTransformSystem>();

            _toDelete.Clear();

            // Client side entities won't need the transform, but that should always be a tiny minority of entities
            var metaQuery = _entityManager.AllEntityQueryEnumerator<MetaDataComponent, TransformComponent>();

            while (metaQuery.MoveNext(out var ent, out var metadata, out var xform))
            {
                var netEnt = metadata.NetEntity;
                if (metadata.NetEntity.IsClientSide())
                {
                    if (deleteClientEntities)
                        _toDelete.Add(ent);

                    continue;
                }

                if (_stateEnts.Contains(netEnt))
                {
                    if (resetAllEntities || metadata.LastStateApplied > state.ToSequence)
                        metadata.LastStateApplied = GameTick.Zero; // TODO track last-state-applied for individual components? Is it even worth it?

                    continue;
                }

                // This entity is going to get deleted, but maybe some if its children won't be, so lets detach them to
                // null. First we will detach the parent in order to reduce the number of broadphase/lookup updates.
                xformSys.DetachParentToNull(ent, xform);

                // Then detach all children.
                foreach (var child in xform._children)
                {
                    xformSys.DetachParentToNull(child, xforms.GetComponent(child), xform);

                    if (deleteClientChildren
                        && !deleteClientEntities // don't add duplicates
                        && _entities.IsClientSide(child))
                    {
                        _toDelete.Add(child);
                    }
                }

                _toDelete.Add(ent);
            }

            foreach (var ent in _toDelete)
            {
                _entities.DeleteEntity(ent);
            }
        }

        private void ProcessDeletions(
            ReadOnlySpan<NetEntity> delSpan,
            EntityQuery<TransformComponent> xforms,
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

            foreach (var netEntity in delSpan)
            {
                // Don't worry about this for later.
                _entityManager.PendingNetEntityStates.Remove(netEntity);

                if (!_entityManager.TryGetEntity(netEntity, out var id))
                    continue;

                if (!xforms.TryGetComponent(id, out var xform))
                    continue; // Already deleted? or never sent to us?

                // First, a single recursive map change
                xformSys.DetachParentToNull(id.Value, xform);

                // Then detach all children.
                var childEnumerator = xform.ChildEnumerator;
                while (childEnumerator.MoveNext(out var child))
                {
                    xformSys.DetachParentToNull(child, xforms.GetComponent(child), xform);
                }

                // Finally, delete the entity.
                _entities.DeleteEntity(id.Value);
            }
            _prof.WriteValue("Count", ProfData.Int32(delSpan.Length));
        }

        public void DetachImmediate(List<NetEntity> entities)
        {
            var metas = _entities.GetEntityQuery<MetaDataComponent>();
            var xforms = _entities.GetEntityQuery<TransformComponent>();
            var xformSys = _entitySystemManager.GetEntitySystem<SharedTransformSystem>();
            var containerSys = _entitySystemManager.GetEntitySystem<ContainerSystem>();
            var lookupSys = _entitySystemManager.GetEntitySystem<EntityLookupSystem>();
            Detach(GameTick.MaxValue, null, entities, metas, xforms, xformSys, containerSys, lookupSys);
        }

        private List<NetEntity> ProcessPvsDeparture(
            GameTick toTick,
            EntityQuery<MetaDataComponent> metas,
            EntityQuery<TransformComponent> xforms,
            SharedTransformSystem xformSys,
            ContainerSystem containerSys,
            EntityLookupSystem lookupSys)
        {
            var toDetach = _processor.GetEntitiesToDetach(toTick, _pvsDetachBudget);
            var detached = new List<NetEntity>();

            if (toDetach.Count == 0)
                return detached;

            // TODO optimize
            // If an entity is leaving PVS, so are all of its children. If we can preserve the hierarchy we can avoid
            // things like container insertion and ejection.

            using var _ = _prof.Group("Leave PVS");
            detached.EnsureCapacity(toDetach.Count);

            foreach (var (tick, ents) in toDetach)
            {
                Detach(tick, toTick, ents, metas, xforms, xformSys, containerSys, lookupSys, detached);
            }

            _prof.WriteValue("Count", ProfData.Int32(detached.Count));
            return detached;
        }

        private void Detach(GameTick maxTick,
            GameTick? lastStateApplied,
            List<NetEntity> entities,
            EntityQuery<MetaDataComponent> metas,
            EntityQuery<TransformComponent> xforms,
            SharedTransformSystem xformSys,
            ContainerSystem containerSys,
            EntityLookupSystem lookupSys,
            List<NetEntity>? detached = null)
        {
            foreach (var netEntity in entities)
            {
                if (!_entityManager.TryGetEntityData(netEntity, out var ent, out var meta))
                    continue;

                if (meta.LastStateApplied > maxTick)
                {
                    // Server sent a new state for this entity sometime after the detach message was sent. The
                    // detach message probably just arrived late or was initially dropped.
                    continue;
                }

                if ((meta.Flags & (MetaDataFlags.Detached | MetaDataFlags.Undetachable)) != 0)
                    continue;

                if (lastStateApplied.HasValue)
                    meta.LastStateApplied = lastStateApplied.Value;

                var xform = xforms.GetComponent(ent.Value);
                if (xform.ParentUid.IsValid())
                {
                    lookupSys.RemoveFromEntityTree(ent.Value, xform);
                    xform.Broadphase = BroadphaseData.Invalid;

                    // In some cursed scenarios an entity inside of a container can leave PVS without the container itself leaving PVS.
                    // In those situations, we need to add the entity back to the list of expected entities after detaching.
                    BaseContainer? container = null;
                    if ((meta.Flags & MetaDataFlags.InContainer) != 0 &&
                        metas.TryGetComponent(xform.ParentUid, out var containerMeta) &&
                        (containerMeta.Flags & MetaDataFlags.Detached) == 0 &&
                        containerSys.TryGetContainingContainer(xform.ParentUid, ent.Value, out container, null, true))
                    {
                        containerSys.Remove((ent.Value, xform, meta), container, false, true);
                    }

                    meta._flags |= MetaDataFlags.Detached;
                    xformSys.DetachParentToNull(ent.Value, xform);
                    DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) == 0);

                    if (container != null)
                        containerSys.AddExpectedEntity(netEntity, container);
                }

                detached?.Add(netEntity);
            }
        }

        private void InitializeAndStart(Dictionary<NetEntity, EntityState> toCreate)
        {
            var metaQuery = _entityManager.GetEntityQuery<MetaDataComponent>();

#if EXCEPTION_TOLERANCE
            var brokenEnts = new List<EntityUid>();
#endif
            using (_prof.Group("Initialize Entity"))
            {
                foreach (var netEntity in toCreate.Keys)
                {
                    var entity = _entityManager.GetEntity(netEntity);
#if EXCEPTION_TOLERANCE
                    try
                    {
#endif
                    _entities.InitializeEntity(entity, metaQuery.GetComponent(entity));
#if EXCEPTION_TOLERANCE
                    }
                    catch (Exception e)
                    {
                        _sawmill.Error($"Server entity threw in Init: ent={_entities.ToPrettyString(entity)}");
                        _runtimeLog.LogException(e, $"{nameof(ClientGameStateManager)}.{nameof(InitializeAndStart)}");
                        brokenEnts.Add(entity);
                        toCreate.Remove(netEntity);
                    }
#endif
                }
            }

            using (_prof.Group("Start Entity"))
            {
                foreach (var netEntity in toCreate.Keys)
                {
                    var entity = _entityManager.GetEntity(netEntity);
#if EXCEPTION_TOLERANCE
                    try
                    {
#endif
                    _entities.StartEntity(entity);
#if EXCEPTION_TOLERANCE
                    }
                    catch (Exception e)
                    {
                        _sawmill.Error($"Server entity threw in Start: ent={_entityManager.ToPrettyString(entity)}");
                        _runtimeLog.LogException(e, $"{nameof(ClientGameStateManager)}.{nameof(InitializeAndStart)}");
                        brokenEnts.Add(entity);
                        toCreate.Remove(netEntity);
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

        private void HandleEntityState(EntityUid uid, NetEntity netEntity, MetaDataComponent meta, IEventBus bus, EntityState? curState,
            EntityState? nextState, GameTick lastApplied, GameTick toTick, bool enteringPvs)
        {
            _compStateWork.Clear();

            // First remove any deleted components
            if (curState?.NetComponents != null)
            {
                _toRemove.Clear();

                foreach (var (id, comp) in meta.NetComponents)
                {
                    DebugTools.Assert(comp.NetSyncEnabled);

                    if (!curState.NetComponents.Contains(id))
                        _toRemove.Add(comp);
                }

                foreach (var comp in _toRemove)
                {
                    _entities.RemoveComponent(uid, comp, meta);
                }
            }

            if (enteringPvs)
            {
                // last-server state has already been updated with new information from curState
                // --> simply reset to the most recent server state.
                //
                // as to why we need to reset: because in the process of detaching to null-space, we will have dirtied
                // the entity. most notably, all entities will have been ejected from their containers.
                foreach (var (id, state) in _processor.GetLastServerStates(netEntity))
                {
                    if (!meta.NetComponents.TryGetValue(id, out var comp))
                    {
                        comp = _compFactory.GetComponent(id);
                        _entityManager.AddComponent(uid, comp, true, metadata: meta);
                    }

                    _compStateWork[id] = (comp, state, null);
                }
            }
            else if (curState != null)
            {
                foreach (var compChange in curState.ComponentChanges.Span)
                {
                    if (!meta.NetComponents.TryGetValue(compChange.NetID, out var comp))
                    {
                        comp = _compFactory.GetComponent(compChange.NetID);
                        _entityManager.AddComponent(uid, comp, true, metadata:meta);
                    }
                    else if (compChange.LastModifiedTick <= lastApplied && lastApplied != GameTick.Zero)
                        continue;

                    _compStateWork[compChange.NetID] = (comp, compChange.State, null);
                }
            }

            if (nextState != null)
            {
                foreach (var compState in nextState.ComponentChanges.Span)
                {
                    if (compState.LastModifiedTick != toTick + 1)
                        continue;

                    if (!meta.NetComponents.TryGetValue(compState.NetID, out var comp))
                    {
                        // The component can be null here due to interp, because the NEXT state will have a new
                        // component, but the component does not yet exist.
                        continue;
                    }

                    ref var state =
                        ref CollectionsMarshal.GetValueRefOrAddDefault(_compStateWork, compState.NetID, out var exists);

                    if (exists)
                        state = (comp, state.curState, compState.State);
                    else
                        state = (comp, null, compState.State);
                }
            }

            // If we have a NetEntity we reference come in then apply their state.
            if (_pendingReapplyNetStates.TryGetValue(uid, out var reapplyTypes))
            {
                var lastState = _processor.GetLastServerStates(netEntity);

                foreach (var type in reapplyTypes)
                {
                    var compRef = _compFactory.GetRegistration(type);
                    var netId = compRef.NetID;

                    if (netId == null)
                        continue;

                    if (!meta.NetComponents.TryGetValue(netId.Value, out var comp) ||
                        !lastState.TryGetValue(netId.Value, out var lastCompState))
                    {
                        continue;
                    }

                    ref var compState =
                        ref CollectionsMarshal.GetValueRefOrAddDefault(_compStateWork, netId.Value, out var exists);

                    if (exists)
                        continue;

                    compState = (comp, lastCompState, null);
                }
            }

            foreach (var (comp, cur, next) in _compStateWork.Values)
            {
                try
                {
                    var handleState = new ComponentHandleState(cur, next);
                    bus.RaiseComponentEvent(comp, ref handleState);
                }
#pragma warning disable CS0168 // Variable is declared but never used
                catch (Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
                {
#if EXCEPTION_TOLERANCE
                    _sawmill.Error($"Failed to apply comp state: entity={_entities.ToPrettyString(uid)}, comp={comp.GetType()}");
                    _runtimeLog.LogException(e, $"{nameof(ClientGameStateManager)}.{nameof(HandleEntityState)}");
#else
                    _sawmill.Error($"Failed to apply comp state: entity={_entities.ToPrettyString(uid)}, comp={comp.GetType()}");
                    throw;
#endif
                }
            }
        }

        #region Debug Commands

        private bool TryParseUid(IConsoleShell shell, string[] args, out EntityUid uid, [NotNullWhen(true)] out MetaDataComponent? meta)
        {
            if (args.Length != 1)
            {
                shell.WriteError(Loc.GetString("cmd-invalid-arg-number-error"));
                uid = EntityUid.Invalid;
                meta = null;
                return false;
            }

            if (!EntityUid.TryParse(args[0], out uid))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-uid", ("arg", args[0])));
                meta = null;
                return false;
            }

            if (!_entities.TryGetComponent(uid, out meta))
            {
                shell.WriteError(Loc.GetString("cmd-parse-failure-entity-exist", ("arg", args[0])));
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Reset an entity to the most recently received server state. This will also reset entities that have been detached to null-space.
        /// </summary>
        private void ResetEntCommand(IConsoleShell shell, string argStr, string[] args)
        {
            if (!TryParseUid(shell, args, out var uid, out var meta))
                return;

            using var _ = _timing.StartStateApplicationArea();
            ResetEnt(uid, meta, false);
        }

        /// <summary>
        ///     Detach an entity to null-space, as if it had left PVS range.
        /// </summary>
        private void DetachEntCommand(IConsoleShell shell, string argStr, string[] args)
        {
            if (!TryParseUid(shell, args, out var uid, out var meta))
                return;

            if ((meta.Flags & MetaDataFlags.Detached) != 0)
                return;

            using var _ = _timing.StartStateApplicationArea();

            meta.Flags |= MetaDataFlags.Detached;

            var containerSys = _entities.EntitySysManager.GetEntitySystem<ContainerSystem>();

            var xform = _entities.GetComponent<TransformComponent>(uid);
            if (xform.ParentUid.IsValid())
            {
                BaseContainer? container = null;
                if ((meta.Flags & MetaDataFlags.InContainer) != 0 &&
                    _entities.TryGetComponent(xform.ParentUid, out MetaDataComponent? containerMeta) &&
                    (containerMeta.Flags & MetaDataFlags.Detached) == 0)
                {
                    containerSys.TryGetContainingContainer(xform.ParentUid, uid, out container, null, true);
                }

                _entities.EntitySysManager.GetEntitySystem<TransformSystem>().DetachParentToNull(uid, xform);

                if (container != null)
                    containerSys.AddExpectedEntity(_entities.GetNetEntity(uid), container);
            }
        }

        /// <summary>
        ///     Deletes an entity. Unlike the normal delete command, this is CLIENT-SIDE.
        /// </summary>
        /// <remarks>
        ///     Unless the entity is a client-side entity, this will likely cause errors.
        /// </remarks>
        private void LocalDeleteEntCommand(IConsoleShell shell, string argStr, string[] args)
        {
            if (!TryParseUid(shell, args, out var uid, out var meta))
                return;

            // If this is not a client-side entity, it also needs to be removed from the full-server state dictionary to
            // avoid errors. This has to be done recursively for all children.
            void _recursiveRemoveState(NetEntity netEntity, TransformComponent xform, EntityQuery<MetaDataComponent> metaQuery, EntityQuery<TransformComponent> xformQuery)
            {
                _processor._lastStateFullRep.Remove(netEntity);
                foreach (var child in xform._children)
                {
                    if (xformQuery.TryGetComponent(child, out var childXform) &&
                        metaQuery.TryGetComponent(child, out var childMeta))
                    {
                        _recursiveRemoveState(childMeta.NetEntity, childXform, metaQuery, xformQuery);
                    }
                }
            }

            if (!_entities.IsClientSide(uid) && _entities.TryGetComponent(uid, out TransformComponent? xform))
                _recursiveRemoveState(meta.NetEntity, xform, _entities.GetEntityQuery<MetaDataComponent>(), _entities.GetEntityQuery<TransformComponent>());

            // Set ApplyingState to true to avoid logging errors about predicting the deletion of networked entities.
            using (_timing.StartStateApplicationArea())
            {
                _entities.DeleteEntity(uid);
            }
        }

        /// <summary>
        ///     Resets all entities to the most recently received server state. This only impacts entities that have not been detached to null-space.
        /// </summary>
        private void ResetAllEnts(IConsoleShell shell, string argStr, string[] args)
        {
            using var _ = _timing.StartStateApplicationArea();

            var query = _entityManager.AllEntityQueryEnumerator<MetaDataComponent>();

            while (query.MoveNext(out var uid, out var meta))
            {
                ResetEnt(uid, meta);
            }
        }

        /// <summary>
        ///     Reset a given entity to the most recent server state.
        /// </summary>
        private void ResetEnt(EntityUid uid, MetaDataComponent meta, bool skipDetached = true)
        {
            if (skipDetached && (meta.Flags & MetaDataFlags.Detached) != 0)
                return;

            meta.Flags &= ~MetaDataFlags.Detached;

            if (!_processor.TryGetLastServerStates(meta.NetEntity, out var lastState))
                return;

            foreach (var (id, state) in lastState)
            {
                if (!meta.NetComponents.TryGetValue(id, out var comp))
                {
                    comp = _compFactory.GetComponent(id);
                    _entityManager.AddComponent(uid, comp, true, meta);
                }

                var handleState = new ComponentHandleState(state, null);
                _entityManager.EventBus.RaiseComponentEvent(comp, ref handleState);
            }

            // ensure we don't have any extra components
            _toRemove.Clear();

            foreach (var (id, comp) in meta.NetComponents)
            {
                if (comp.NetSyncEnabled && !lastState.ContainsKey(id))
                    _toRemove.Add(comp);
            }

            foreach (var comp in _toRemove)
            {
                _entities.RemoveComponent(uid, comp);
            }
        }
        #endregion

        public bool IsQueuedForDetach(NetEntity entity)
            => _processor.IsQueuedForDetach(entity);
    }

    public sealed class GameStateAppliedArgs : EventArgs
    {
        public GameState AppliedState { get; }
        public readonly List<NetEntity> Detached;

        public GameStateAppliedArgs(GameState appliedState, List<NetEntity> detached)
        {
            AppliedState = appliedState;
            Detached = detached;
        }
    }

    public sealed class MissingMetadataException : Exception
    {
        public readonly NetEntity NetEntity;

        public MissingMetadataException(NetEntity netEntity)
            : base($"Server state is missing the metadata component for a new entity: {netEntity}.")
        {
            NetEntity = netEntity;
        }
    }
}
