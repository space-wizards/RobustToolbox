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
using Robust.Shared.Collections;
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

        private readonly Queue<(uint sequence, GameTick sourceTick, object msg, object sessionMsg)>
            _pendingSystemMessages
                = new();

        // Game state dictionaries that get used every tick.
        private readonly Dictionary<EntityUid, StateData> _toApply = new();
        private StateData[] _toApplySorted = default!;
        private readonly Dictionary<ushort, (IComponent Component, IComponentState? curState, IComponentState? nextState)> _compStateWork = new();
        private readonly Dictionary<EntityUid, HashSet<Type>> _pendingReapplyNetStates = new();
        private readonly HashSet<NetEntity> _stateEnts = new();
        private readonly List<EntityUid> _toDelete = new();
        private readonly List<IComponent> _toRemove = new();
        private readonly Dictionary<NetEntity, Dictionary<ushort, IComponentState?>> _outputData = new();
        private readonly List<(EntityUid, TransformComponent)> _queuedBroadphaseUpdates = new();
        private readonly HashSet<EntityUid> _sorted = new();
        private readonly List<NetEntity> _created = new();
        private readonly List<NetEntity> _detached = new();

        private readonly record struct StateData(
            EntityUid Uid,
            NetEntity NetEntity,
            MetaDataComponent Meta,
            bool Created,
            bool EnteringPvs,
            GameTick LastApplied,
            EntityState? CurState,
            EntityState? NextState,
            HashSet<Type>? PendingReapply);

        private readonly ObjectPool<Dictionary<ushort, IComponentState?>> _compDataPool =
            new DefaultObjectPool<Dictionary<ushort, IComponentState?>>(new DictPolicy<ushort, IComponentState?>(), 256);

        private uint _metaCompNetId;
        private uint _xformCompNetId;

        [Dependency] private readonly IReplayRecordingManager _replayRecording = default!;
        [Dependency] private readonly IComponentFactory _compFactory = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IClientNetManager _network = default!;
        [Dependency] private readonly IBaseClient _client = default!;
        [Dependency] private readonly IClientGameTiming _timing = default!;
        [Dependency] private readonly INetConfigurationManager _config = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IConsoleHost _conHost = default!;
        [Dependency] private readonly ClientEntityManager _entities = default!;
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
        private readonly List<EntityUid> _brokenEnts = new();

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

            var xformId = _compFactory.GetRegistration(typeof(TransformComponent)).NetID;
            if (!xformId.HasValue)
                throw new InvalidOperationException("TransformComponent does not have a NetId.");

            _xformCompNetId = xformId.Value;
        }

        private void OnComponentAdded(AddedComponentEventArgs args)
        {
            if (!_resettingPredictedEntities)
                return;

            var comp = args.ComponentType;
            if (comp.NetID == null)
                return;

            if (_entities.IsClientSide(args.BaseArgs.Owner))
                return;

            _sawmill.Error($"""
                Added component {comp.Name} to entity {_entities.ToPrettyString(args.BaseArgs.Owner)} while resetting predicted entities.
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

        public Dictionary<NetEntity, Dictionary<ushort, IComponentState?>> GetFullRep()
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
                    ApplyGameState(curState, nextState);
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
                    MergeImplicitData();
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
                _entities.TickUpdate((float) _timing.TickPeriod.TotalSeconds, noPredictions: !IsPredictionEnabled, histogram: null);
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
                    _entities.EventBus.RaiseEvent(EventSource.Local, pendingMessagesEnumerator.Current.msg);
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
            var metaQuery = _entities.GetEntityQuery<MetaDataComponent>();
            RemQueue<IComponent> toRemove = new();

            // Handle predicted entity spawns.
            var predicted = new ValueList<EntityUid>();
            var predictedQuery = _entities.AllEntityQueryEnumerator<PredictedSpawnComponent>();

            while (predictedQuery.MoveNext(out var uid, out var _))
            {
                predicted.Add(uid);
            }

            // Entity will get re-created as part of the tick.
            foreach (var ent in predicted)
            {
                _entities.DeleteEntity(ent);
            }

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

                        if (compState != null)
                        {
                            var handleState = new ComponentHandleState(compState, null);
                            _entities.EventBus.RaiseComponentEvent(entity, comp, ref handleState);
                        }

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

                        var comp = _entities.AddComponent(entity, netId, meta);

                        if (_sawmill.Level <= LogLevel.Debug)
                            _sawmill.Debug($"  A component was removed: {comp.GetType()}");

                        if (state != null)
                        {
                            var stateEv = new ComponentHandleState(state, null);
                            _entities.EventBus.RaiseComponentEvent(entity, comp, ref stateEv);
                        }

                        comp.ClearCreationTick(); // don't undo the re-adding.
                        comp.LastModifiedTick = _timing.LastRealTick;
                    }
                }

                DebugTools.Assert(meta.EntityLastModifiedTick > _timing.LastRealTick);
                meta.EntityLastModifiedTick = _timing.LastRealTick;
            }

            _entities.System<PhysicsSystem>().ResetContacts();

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
        public void MergeImplicitData()
        {
            var bus = _entities.EventBus;

            foreach (var netEntity in _created)
            {
                if (!_entities.TryGetEntityData(netEntity, out _, out var meta))
                {
                    _sawmill.Error($"Encountered deleted entity while merging implicit data! NetEntity: {netEntity}");

#if !EXCEPTION_TOLERANCE
                    throw new KeyNotFoundException();
#else
                    continue;
#endif
                }

                var compData = _compDataPool.Get();
                _outputData.Add(netEntity, compData);

                foreach (var (netId, component) in meta.NetComponents)
                {
                    DebugTools.Assert(component.NetSyncEnabled);

                    var state = _entities.GetComponentState(bus, component, null, GameTick.Zero);
                    DebugTools.Assert(state is not IComponentDeltaState);
                    compData.Add(netId, state);
                }
            }

            _created.Clear();
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

            using (_prof.Group("Entity"))
            {
                ApplyEntityStates(curState, nextState);
            }

            using (_prof.Group("Player"))
            {
                _players.ApplyPlayerStates(curState.PlayerStates.Value ?? Array.Empty<SessionState>());
            }

            using (_prof.Group("Callback"))
            {
                GameStateApplied?.Invoke(new GameStateAppliedArgs(curState, _detached));
            }

            return _created;
        }

        private void ApplyEntityStates(GameState curState, GameState? nextState)
        {
            var metas = _entities.GetEntityQuery<MetaDataComponent>();
            var xforms = _entities.GetEntityQuery<TransformComponent>();
            var xformSys = _entitySystemManager.GetEntitySystem<SharedTransformSystem>();

            var enteringPvs = 0;
            _toApply.Clear();
            _created.Clear();
            _pendingReapplyNetStates.Clear();
            var curSpan = curState.EntityStates.Span;

            // Create new entities
            // This is done BEFORE state application to ensure any new parents exist before existing children have their states applied, otherwise, we may have issues with entity transforms!

            using (_prof.Group("Create uninitialized entities"))
            {
                var created = 0;
                foreach (var es in curSpan)
                {
                    if (_entities.TryGetEntity(es.NetEntity, out var nUid))
                    {
                        DebugTools.Assert(_entities.EntityExists(nUid));
                        continue;
                    }

                    created++;
                    CreateNewEntity(es, curState.ToSequence);
                }

                _prof.WriteValue("Count", ProfData.Int32(created));
            }

            // Add entity entities that aren't new to _toCreate.
            // In the process, we also check if these entities are re-entering PVS range.
            foreach (var es in curSpan)
            {
                if (!_entities.TryGetEntityData(es.NetEntity, out var uid, out var meta))
                    continue;

                var isEnteringPvs = (meta.Flags & MetaDataFlags.Detached) != 0;
                if (isEnteringPvs)
                {
                    // _toApply already contains newly created entities, but these should never be "entering PVS"
                    DebugTools.Assert(!_toApply.ContainsKey(uid.Value));

                    meta.Flags &= ~MetaDataFlags.Detached;
                    enteringPvs++;
                }
                else if (meta.LastStateApplied >= es.EntityLastModified && meta.LastStateApplied != GameTick.Zero)
                {
                    // _toApply already contains newly created entities, but for those this set should have no effect
                    DebugTools.Assert(!_toApply.ContainsKey(uid.Value) || meta.LastStateApplied == curState.ToSequence);

                    meta.LastStateApplied = curState.ToSequence;
                    continue;
                }

                // Any newly created entities already added to _toApply should've already been caught by the previous continue
                DebugTools.Assert(!_toApply.ContainsKey(uid.Value));

                _toApply.Add(uid.Value, new(uid.Value, es.NetEntity, meta, false, isEnteringPvs, meta.LastStateApplied, es, null, null));
                meta.LastStateApplied = curState.ToSequence;
            }

            // Detach entities to null space
            var containerSys = _entitySystemManager.GetEntitySystem<ContainerSystem>();
            var lookupSys = _entitySystemManager.GetEntitySystem<EntityLookupSystem>();
            ProcessPvsDeparture(curState.ToSequence, metas, xforms, xformSys, containerSys, lookupSys);

            // Check next state (AFTER having created new entities introduced in curstate)
            if (nextState != null)
            {
                foreach (var es in nextState.EntityStates.Span)
                {
                    if (!_entities.TryGetEntityData(es.NetEntity, out var uid, out var meta))
                        continue;

                    // Does the next state actually have any future information about this entity that could be used for interpolation?
                    if (es.EntityLastModified != nextState.ToSequence)
                        continue;

                    ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(_toApply, uid.Value, out var exists);

                    state = exists
                        ? state with {NextState = es}
                        : new(uid.Value, es.NetEntity, meta, false, false, GameTick.Zero, null, es, null);
                }
            }

            // Check pending states and see if we need to force any entities to re-run component states.
            foreach (var (uid, pending) in _pendingReapplyNetStates)
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

                ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(_toApply, uid, out var exists);

                state = exists
                    ? state with {PendingReapply = pending}
                    : new(uid, meta.NetEntity, meta, false, false, GameTick.Zero, null, null, pending);
            }

            _queuedBroadphaseUpdates.Clear();

            using (_prof.Group("Sort States"))
            {
                SortStates(_toApply);
            }

            // Apply entity states.
            using (_prof.Group("Apply States"))
            {
                var span = _toApplySorted.AsSpan(0, _toApply.Count);
                foreach (ref var data in span)
                {
                    ApplyEntState(data, curState.ToSequence);
                }

                Array.Clear(_toApplySorted, 0, _toApply.Count);
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
                    ProcessDeletions(delSpan, xforms, metas, xformSys);
                }
                catch (Exception e)
                {
                    _sawmill.Error($"Caught exception while deleting entities");
                    _runtimeLog.LogException(e, $"{nameof(ClientGameStateManager)}.{nameof(ApplyEntityStates)}");
                }
            }

            // Delete any entities that failed to properly initialize/start
            foreach (var entity in _brokenEnts)
            {
                _entities.DeleteEntity(entity);
            }

            _brokenEnts.Clear();

            _prof.WriteValue("State Size", ProfData.Int32(curSpan.Length));
            _prof.WriteValue("Entered PVS", ProfData.Int32(enteringPvs));
        }

        private void ApplyEntState(in StateData data, GameTick toTick)
        {
            try
            {
                HandleEntityState(data, _entities.EventBus, toTick);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Caught exception while applying entity state. Entity: {_entities.ToPrettyString(data.Uid)}. Exception: {e}");
                _brokenEnts.Add(data.Uid);
                RequestFullState();
#if !EXCEPTION_TOLERANCE
                throw;
#else
                return;
#endif
            }

            if (data.Created)
            {
                try
                {
                    _entities.InitializeEntity(data.Uid, data.Meta);
                    _entities.StartEntity(data.Uid);
                }
                catch (Exception e)
                {
                    _sawmill.Error(
                        $"Caught exception while initializing or starting entity: {_entities.ToPrettyString(data.Uid)}. Exception: {e}");
                    _brokenEnts.Add(data.Uid);
                    RequestFullState();
#if !EXCEPTION_TOLERANCE
                    throw;
#else
                    return;
#endif
                }
            }

            if (!data.EnteringPvs)
                return;

            // Now that things like collision data, fixtures, and positions have been updated, we queue a
            // broadphase update. However, if this entity is parented to some other entity also re-entering PVS,
            // we only need to update it's parent (as it recursively updates children anyways).
            var xform = _entities.TransformQuery.Comp(data.Uid);
            DebugTools.Assert(xform.Broadphase == BroadphaseData.Invalid);
            xform.Broadphase = null;
            if (!_toApply.TryGetValue(xform.ParentUid, out var parent) || !parent.EnteringPvs)
                _queuedBroadphaseUpdates.Add((data.Uid, xform));
        }

        private void CreateNewEntity(EntityState state, GameTick toTick)
        {
            // TODO GAME STATE
            // store MetaData & Transform information separately.
            var metaState =
                (MetaDataComponentState?) state.ComponentChanges.Value?.FirstOrDefault(c => c.NetID == _metaCompNetId)
                    .State;

            if (metaState == null)
                throw new MissingMetadataException(state.NetEntity);

            var uid = _entities.CreateEntity(metaState.PrototypeId, out var newMeta);
            _toApply.Add(uid, new(uid, state.NetEntity, newMeta, true, false, GameTick.Zero, state, null, null));
            _created.Add(state.NetEntity);

            // Client creates a client-side net entity for the newly created entity.
            // We need to clear this mapping before assigning the real net id.
            // TODO NetEntity Jank: prevent the client from creating this in the first place.
            _entities.ClearNetEntity(newMeta.NetEntity);

            _entities.SetNetEntity(uid, state.NetEntity, newMeta);
            newMeta.LastStateApplied = toTick;

            // Check if there's any component states awaiting this entity.
            if (!_entities.PendingNetEntityStates.Remove(state.NetEntity, out var value))
                return;

            foreach (var (type, owner) in value)
            {
                var pending = _pendingReapplyNetStates.GetOrNew(owner);
                pending.Add(type);
            }
        }

        /// <summary>
        /// Sort states to ensure that we always apply states, initialize, and start parent entities before any of their
        /// children.
        /// </summary>
        private void SortStates(Dictionary<EntityUid, StateData> toApply)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (_toApplySorted == null || _toApplySorted.Length < toApply.Count)
                Array.Resize(ref _toApplySorted, toApply.Count);

            _sorted.Clear();

            var i = 0;
            foreach (var (ent, data) in toApply)
            {
                AddToSorted(ent, data, ref i);
            }

            DebugTools.AssertEqual(i, toApply.Count);
        }

        private void AddToSorted(EntityUid ent, in StateData data, ref int i)
        {
            if (!_sorted.Add(ent))
                return;

            EnsureParentsSorted(ent, data, ref i);
            _toApplySorted[i++] = data;
        }

        private void EnsureParentsSorted(EntityUid ent, in StateData data, ref int i)
        {
            var parent = GetStateParent(ent, data);

            while (parent != EntityUid.Invalid)
            {
                if (_toApply.TryGetValue(parent, out var parentData))
                {
                    AddToSorted(parent, parentData, ref i);
                    // The above method will handle the rest of the transform hierarchy, so we can just return early.
                    return;
                }

                parent = _entities.TransformQuery.GetComponent(parent).ParentUid;
            }
        }

        /// <summary>
        /// Get the entity's parent in the game state that is being applies. I.e., if the state contains a new
        /// transform state, get the parent from that. Otherwise, return the entity's current parent.
        /// </summary>
        private EntityUid GetStateParent(EntityUid uid, in StateData data)
        {
            // TODO GAME STATE
            // store MetaData & Transform information separately.
            if (data.CurState != null
                && data.CurState.ComponentChanges.Value
                    .TryFirstOrNull(c => c.NetID == _xformCompNetId, out var found))
            {
                var state = (TransformComponentState) found.Value.State!;
                return _entities.GetEntity(state.ParentID);
            }

            return _entities.TransformQuery.GetComponent(uid).ParentUid;
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
            var metas = _entities.GetEntityQuery<MetaDataComponent>();
            var xformSys = _entitySystemManager.GetEntitySystem<SharedTransformSystem>();

            _toDelete.Clear();

            // Client side entities won't need the transform, but that should always be a tiny minority of entities
            var metaQuery = _entities.AllEntityQueryEnumerator<MetaDataComponent, TransformComponent>();

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
                xformSys.DetachEntity(ent, xform);

                // Then detach all children.
                foreach (var child in xform._children)
                {
                    xformSys.DetachEntity(child, xforms.Get(child), metas.Get(child), xform);

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

        private void ProcessDeletions(ReadOnlySpan<NetEntity> delSpan,
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

            foreach (var netEntity in delSpan)
            {
                // Don't worry about this for later.
                _entities.PendingNetEntityStates.Remove(netEntity);

                if (!_entities.TryGetEntity(netEntity, out var id))
                    continue;

                if (!xforms.TryGetComponent(id, out var xform))
                    continue; // Already deleted? or never sent to us?

                // First, a single recursive map change
                xformSys.DetachEntity(id.Value, xform);

                // Then detach all children.
                var childEnumerator = xform.ChildEnumerator;
                while (childEnumerator.MoveNext(out var child))
                {
                    xformSys.DetachEntity(child, xforms.Get(child), metas.Get(child), xform);
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
            _detached.Clear();
        }

        private void ProcessPvsDeparture(
            GameTick toTick,
            EntityQuery<MetaDataComponent> metas,
            EntityQuery<TransformComponent> xforms,
            SharedTransformSystem xformSys,
            ContainerSystem containerSys,
            EntityLookupSystem lookupSys)
        {
            var toDetach = _processor.GetEntitiesToDetach(toTick, _pvsDetachBudget);

            if (toDetach.Count == 0)
                return;

            // TODO optimize
            // If an entity is leaving PVS, so are all of its children. If we can preserve the hierarchy we can avoid
            // things like container insertion and ejection.

            using var _ = _prof.Group("Leave PVS");

            _detached.Clear();
            foreach (var (tick, ents) in toDetach)
            {
                Detach(tick, toTick, ents, metas, xforms, xformSys, containerSys, lookupSys);
            }

            _prof.WriteValue("Count", ProfData.Int32(_detached.Count));
        }

        private void Detach(GameTick maxTick,
            GameTick? lastStateApplied,
            List<NetEntity> entities,
            EntityQuery<MetaDataComponent> metas,
            EntityQuery<TransformComponent> xforms,
            SharedTransformSystem xformSys,
            ContainerSystem containerSys,
            EntityLookupSystem lookupSys)
        {
            foreach (var netEntity in entities)
            {
                if (!_entities.TryGetEntityData(netEntity, out var ent, out var meta))
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
                        containerSys.TryGetContainingContainer(xform.ParentUid, ent.Value, out container))
                    {
                        containerSys.Remove((ent.Value, xform, meta), container, false, true);
                    }

                    meta._flags |= MetaDataFlags.Detached;
                    xformSys.DetachEntity(ent.Value, xform);
                    DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) == 0);

                    if (container != null)
                        containerSys.AddExpectedEntity(netEntity, container);
                }

                _detached.Add(netEntity);
            }
        }

        private void HandleEntityState(in StateData data, IEventBus bus, GameTick toTick)
        {
            _compStateWork.Clear();

            // First remove any deleted components
            if (data.CurState?.NetComponents is {} netComps)
            {
                _toRemove.Clear();

                foreach (var (id, comp) in data.Meta.NetComponents)
                {
                    DebugTools.Assert(comp.NetSyncEnabled);

                    if (!netComps.Contains(id))
                        _toRemove.Add(comp);
                }

                foreach (var comp in _toRemove)
                {
                    _entities.RemoveComponent(data.Uid, comp, data.Meta);
                }
            }

            if (data.EnteringPvs)
            {
                // last-server state has already been updated with new information from curState
                // --> simply reset to the most recent server state.
                //
                // as to why we need to reset: because in the process of detaching to null-space, we will have dirtied
                // the entity. most notably, all entities will have been ejected from their containers.
                foreach (var (id, state) in _processor.GetLastServerStates(data.NetEntity))
                {
                    if (!data.Meta.NetComponents.TryGetValue(id, out var comp))
                    {
                        comp = _compFactory.GetComponent(id);
                        _entities.AddComponent(data.Uid, comp, true, metadata: data.Meta);
                    }

                    _compStateWork[id] = (comp, state, null);
                }
            }
            else if (data.CurState != null)
            {
                foreach (var compChange in data.CurState.ComponentChanges.Span)
                {
                    if (!data.Meta.NetComponents.TryGetValue(compChange.NetID, out var comp))
                    {
                        comp = _compFactory.GetComponent(compChange.NetID);
                        _entities.AddComponent(data.Uid, comp, true, metadata: data.Meta);
                    }
                    else if (compChange.LastModifiedTick <= data.LastApplied && data.LastApplied != GameTick.Zero)
                        continue;

                    _compStateWork[compChange.NetID] = (comp, compChange.State, null);
                }
            }

            if (data.NextState != null)
            {
                foreach (var compState in data.NextState.ComponentChanges.Span)
                {
                    if (compState.LastModifiedTick != toTick + 1)
                        continue;

                    if (!data.Meta.NetComponents.TryGetValue(compState.NetID, out var comp))
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
            DebugTools.Assert(_pendingReapplyNetStates.ContainsKey(data.Uid) == (data.PendingReapply != null));
            if (data.PendingReapply is {} reapplyTypes)
            {
                var lastState = _processor.GetLastServerStates(data.NetEntity);

                foreach (var type in reapplyTypes)
                {
                    var compRef = _compFactory.GetRegistration(type);
                    var netId = compRef.NetID;

                    if (netId == null)
                        continue;

                    if (!data.Meta.NetComponents.TryGetValue(netId.Value, out var comp) ||
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
                if (cur == null && next == null)
                    continue;

                var handleState = new ComponentHandleState(cur, next);
                bus.RaiseComponentEvent(data.Uid, comp, ref handleState);
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
                    containerSys.TryGetContainingContainer(xform.ParentUid, uid, out container);
                }

                _entities.EntitySysManager.GetEntitySystem<TransformSystem>().DetachEntity(uid, xform);

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

            var query = _entities.AllEntityQueryEnumerator<MetaDataComponent>();

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
                    _entities.AddComponent(uid, comp, true, meta);
                }

                if (state == null)
                    continue;

                var handleState = new ComponentHandleState(state, null);
                _entities.EventBus.RaiseComponentEvent(uid, comp, ref handleState);
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
