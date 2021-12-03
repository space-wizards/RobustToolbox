// ReSharper disable once RedundantUsingDirective
// Used in EXCEPTION_TOLERANCE preprocessor
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Map;
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
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameStates
{
    /// <inheritdoc />
    [UsedImplicitly]
    public class ClientGameStateManager : IClientGameStateManager
    {
        private GameStateProcessor _processor = default!;

        private uint _nextInputCmdSeq = 1;
        private readonly Queue<FullInputCmdMessage> _pendingInputs = new();

        private readonly Queue<(uint sequence, GameTick sourceTick, EntityEventArgs msg, object sessionMsg)>
            _pendingSystemMessages
                = new();

        private readonly Dictionary<EntityUid, MapId> _hiddenEntities = new();

        private uint _metaCompNetId;

        [Dependency] private readonly IComponentFactory _compFactory = default!;
        [Dependency] private readonly IClientEntityManagerInternal _entities = default!;
        [Dependency] private readonly IEntityLookup _lookup = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IClientNetManager _network = default!;
        [Dependency] private readonly IBaseClient _client = default!;
        [Dependency] private readonly IClientMapManager _mapManager = default!;
        [Dependency] private readonly IClientGameTiming _timing = default!;
        [Dependency] private readonly INetConfigurationManager _config = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IClientEntityManager _entityManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
#if EXCEPTION_TOLERANCE
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
#endif

        /// <inheritdoc />
        public int MinBufferSize => _processor.MinBufferSize;

        /// <inheritdoc />
        public int TargetBufferSize => _processor.TargetBufferSize;

        /// <inheritdoc />
        public int CurrentBufferSize => _processor.CalculateBufferSize(CurServerTick);

        public bool Predicting { get; private set; }

        public int PredictTickBias { get; private set; }
        public float PredictLagBias { get; private set; }

        public int StateBufferMergeThreshold { get; private set; }

        private uint _lastProcessedSeq;
        private GameTick _lastProcessedTick = GameTick.Zero;

        public GameTick CurServerTick => _lastProcessedTick;

        /// <inheritdoc />
        public event Action<GameStateAppliedArgs>? GameStateApplied;

        /// <inheritdoc />
        public void Initialize()
        {
            _processor = new GameStateProcessor(_timing);

            _network.RegisterNetMessage<MsgState>(HandleStateMessage);
            _network.RegisterNetMessage<MsgStateAck>();
            _client.RunLevelChanged += RunLevelChanged;

            _config.OnValueChanged(CVars.NetInterp, b => _processor.Interpolation = b, true);
            _config.OnValueChanged(CVars.NetInterpRatio, i => _processor.InterpRatio = i, true);
            _config.OnValueChanged(CVars.NetLogging, b => _processor.Logging = b, true);
            _config.OnValueChanged(CVars.NetPredict, b => Predicting = b, true);
            _config.OnValueChanged(CVars.NetPredictTickBias, i => PredictTickBias = i, true);
            _config.OnValueChanged(CVars.NetPredictLagBias, i => PredictLagBias = i, true);
            _config.OnValueChanged(CVars.NetStateBufMergeThreshold, i => StateBufferMergeThreshold = i, true);

            _processor.Interpolation = _config.GetCVar(CVars.NetInterp);
            _processor.InterpRatio = _config.GetCVar(CVars.NetInterpRatio);
            _processor.Logging = _config.GetCVar(CVars.NetLogging);
            Predicting = _config.GetCVar(CVars.NetPredict);
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

            _lastProcessedTick = GameTick.Zero;
            _lastProcessedSeq = 0;
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
            if (!Predicting)
            {
                return;
            }

            message.InputSequence = _nextInputCmdSeq;
            _pendingInputs.Enqueue(message);

            var inputMan = IoCManager.Resolve<IInputManager>();
            inputMan.NetworkBindMap.TryGetKeyFunction(message.InputFunctionId, out var boundFunc);
            Logger.DebugS(CVars.NetPredict.Name,
                $"CL> SENT tick={_timing.CurTick}, sub={_timing.TickFraction}, seq={_nextInputCmdSeq}, func={boundFunc.FunctionName}, state={message.State}");
            _nextInputCmdSeq++;
        }

        public uint SystemMessageDispatched<T>(T message) where T : EntityEventArgs
        {
            if (!Predicting)
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

            // We temporarily change CurTick here so the GameStateProcessor gets the right values.
            var lastCurTick = _timing.CurTick;
            _timing.CurTick = _lastProcessedTick + 1;

            _processor.AddNewState(state);

            // we always ack everything we receive, even if it is late
            AckGameState(state.ToSequence);

            // And reset CurTick to what it was.
            _timing.CurTick = lastCurTick;
        }

        /// <inheritdoc />
        public void ApplyGameState()
        {
            // Calculate how many states we need to apply this tick.
            // Always at least one, but can be more based on StateBufferMergeThreshold.
            var curBufSize = _processor.CurrentBufferSize;
            var targetBufSize = _processor.TargetBufferSize;
            var applyCount = Math.Max(1, curBufSize - targetBufSize - StateBufferMergeThreshold);

            // Logger.Debug(applyCount.ToString());

            var i = 0;
            for (; i < applyCount; i++)
            {
                _timing.LastRealTick = _timing.CurTick = _lastProcessedTick + 1;

                // TODO: We could theoretically communicate with the GameStateProcessor better here.
                // Since game states are sliding windows, it is possible that we need less than applyCount applies here.
                // Consider, if you have 3 states, (tFrom=1, tTo=2), (tFrom=1, tTo=3), (tFrom=2, tTo=3),
                // you only need to apply the last 2 states to go from 1 -> 3.
                // instead of all 3.
                // This would be a nice optimization though also minor since the primary cost here
                // is avoiding entity system and re-prediction runs.
                if (!_processor.ProcessTickStates(_timing.CurTick, out var curState, out var nextState))
                {
                    break;
                }

                // Logger.DebugS("net", $"{IGameTiming.TickStampStatic}: applying state from={curState.FromSequence} to={curState.ToSequence} ext={curState.Extrapolated}");

                // TODO: If Predicting gets disabled *while* the world state is dirty from a prediction,
                // this won't run meaning it could potentially get stuck dirty.
                if (Predicting && i == 0)
                {
                    // Disable IsFirstTimePredicted while re-running HandleComponentState here.
                    // Helps with debugging.
                    using var resetArea = _timing.StartPastPredictionArea();

                    ResetPredictedEntities(_timing.CurTick);
                }

                if (!curState.Extrapolated)
                {
                    _processor.UpdateFullRep(curState);
                }

                // Store last tick we got from the GameStateProcessor.
                _lastProcessedTick = _timing.CurTick;

                // apply current state
                var createdEntities = ApplyGameState(curState, nextState);

                MergeImplicitData(createdEntities);

                if (_lastProcessedSeq < curState.LastProcessedInput)
                {
                    Logger.DebugS(CVars.NetPredict.Name, $"SV> RCV  tick={_timing.CurTick}, seq={_lastProcessedSeq}");
                    _lastProcessedSeq = curState.LastProcessedInput;
                }
            }

            if (i == 0)
            {
                // Didn't apply a single state successfully.
                return;
            }

            var input = _entitySystemManager.GetEntitySystem<InputSystem>();

            // remove old pending inputs
            while (_pendingInputs.Count > 0 && _pendingInputs.Peek().InputSequence <= _lastProcessedSeq)
            {
                var inCmd = _pendingInputs.Dequeue();

                _inputManager.NetworkBindMap.TryGetKeyFunction(inCmd.InputFunctionId, out var boundFunc);
                Logger.DebugS(CVars.NetPredict.Name,
                    $"SV>     seq={inCmd.InputSequence}, func={boundFunc.FunctionName}, state={inCmd.State}");
            }

            while (_pendingSystemMessages.Count > 0 && _pendingSystemMessages.Peek().sequence <= _lastProcessedSeq)
            {
                _pendingSystemMessages.Dequeue();
            }

            DebugTools.Assert(_timing.InSimulation);

            if (Predicting)
            {
                using var _ = _timing.StartPastPredictionArea();

                if (_pendingInputs.Count > 0)
                {
                    Logger.DebugS(CVars.NetPredict.Name,  "CL> Predicted:");
                }

                var pendingInputEnumerator = _pendingInputs.GetEnumerator();
                var pendingMessagesEnumerator = _pendingSystemMessages.GetEnumerator();
                var hasPendingInput = pendingInputEnumerator.MoveNext();
                var hasPendingMessage = pendingMessagesEnumerator.MoveNext();

                var ping = _network.ServerChannel!.Ping / 1000f + PredictLagBias; // seconds.
                var targetTick = _timing.CurTick.Value + _processor.TargetBufferSize +
                                 (int) Math.Ceiling(_timing.TickRate * ping) + PredictTickBias;

                // Logger.DebugS("net.predict", $"Predicting from {_lastProcessedTick} to {targetTick}");

                for (var t = _lastProcessedTick.Value + 1; t <= targetTick; t++)
                {
                    var tick = new GameTick(t);
                    _timing.CurTick = tick;

                    while (hasPendingInput && pendingInputEnumerator.Current.Tick <= tick)
                    {
                        var inputCmd = pendingInputEnumerator.Current;

                        _inputManager.NetworkBindMap.TryGetKeyFunction(inputCmd.InputFunctionId, out var boundFunc);

                        Logger.DebugS(CVars.NetPredict.Name,
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

                    if (t != targetTick)
                    {
                        // Don't run EntitySystemManager.TickUpdate if this is the target tick,
                        // because the rest of the main loop will call into it with the target tick later,
                        // and it won't be a past prediction.
                        _entitySystemManager.TickUpdate((float) _timing.TickPeriod.TotalSeconds);
                        ((IBroadcastEventBusInternal) _entities.EventBus).ProcessEventQueue();
                    }
                }
            }

            _entities.TickUpdate((float) _timing.TickPeriod.TotalSeconds);

            _lookup.Update();
        }

        private void ResetPredictedEntities(GameTick curTick)
        {
            foreach (var entity in _entities.GetEntities())
            {
                // TODO: 99% there's an off-by-one here.
                if (entity.Uid.IsClientSide() || IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity.Uid).EntityLastModifiedTick < curTick)
                {
                    continue;
                }

                Logger.DebugS(CVars.NetPredict.Name, $"Entity {entity.Uid} was made dirty.");

                if (!_processor.TryGetLastServerStates(entity.Uid, out var last))
                {
                    // Entity was probably deleted on the server so do nothing.
                    continue;
                }

                // TODO: handle component deletions/creations.
                foreach (var (netId, comp) in _entityManager.GetNetComponents(entity.Uid))
                {
                    DebugTools.AssertNotNull(netId);

                    if (comp.LastModifiedTick < curTick || !last.TryGetValue(netId, out var compState))
                    {
                        continue;
                    }

                    Logger.DebugS(CVars.NetPredict.Name, $"  And also its component {comp.Name}");
                    // TODO: Handle interpolation.
                    var handleState = new ComponentHandleState(compState, null);
                    _entities.EventBus.RaiseComponentEvent(comp, ref handleState);
                    comp.HandleComponentState(compState, null);
                }
            }
        }

        private void MergeImplicitData(List<EntityUid> createdEntities)
        {
            // The server doesn't send data that the server can replicate itself on entity creation.
            // As such, GameStateProcessor doesn't have that data either.
            // We have to feed it back this data by calling GetComponentState() and such,
            // so that we can later roll back to it (if necessary).
            var outputData = new Dictionary<EntityUid, Dictionary<uint, ComponentState>>();

            Debug.Assert(_players.LocalPlayer != null, "_players.LocalPlayer != null");

            var bus = _entityManager.EventBus;

            foreach (var createdEntity in createdEntities)
            {
                var compData = new Dictionary<uint, ComponentState>();
                outputData.Add(createdEntity, compData);

                foreach (var (netId, component) in _entityManager.GetNetComponents(createdEntity))
                {
                    var state = _entityManager.GetComponentState(bus, component);

                    if(state.GetType() == typeof(ComponentState))
                        continue;

                    compData.Add(netId, state);
                }
            }

            _processor.MergeImplicitData(outputData);
        }

        private void AckGameState(GameTick sequence)
        {
            var msg = _network.CreateNetMessage<MsgStateAck>();
            msg.Sequence = sequence;
            _network.ClientSendMessage(msg);
        }

        private List<EntityUid> ApplyGameState(GameState curState, GameState? nextState)
        {
            _config.TickProcessMessages();
            _mapManager.ApplyGameStatePre(curState.MapData, curState.EntityStates.Span);
            var createdEntities = ApplyEntityStates(curState.EntityStates.Span, curState.EntityDeletions.Span,
                nextState != null ? nextState.EntityStates.Span : default);
            _players.ApplyPlayerStates(curState.PlayerStates.Value ?? Array.Empty<PlayerState>());
            _mapManager.ApplyGameStatePost(curState.MapData);

            GameStateApplied?.Invoke(new GameStateAppliedArgs(curState));
            return createdEntities;
        }

        private List<EntityUid> ApplyEntityStates(ReadOnlySpan<EntityState> curEntStates, ReadOnlySpan<EntityUid> deletions,
            ReadOnlySpan<EntityState> nextEntStates)
        {
            var toApply = new Dictionary<IEntity, (EntityState?, EntityState?)>();
            var toInitialize = new List<IEntity>();
            var created = new List<EntityUid>();
            var toHide = new List<EntityUid>();
            var toShow = new List<EntityUid>();

            foreach (var es in curEntStates)
            {
                EntityUid uid;
                //Known entities
                if (_entities.TryGetEntity(es.Uid, out var entity))
                {
                    // Logger.Debug($"[{IGameTiming.TickStampStatic}] MOD {es.Uid}");
                    toApply.Add(entity, (es, null));
                    if(_hiddenEntities.ContainsKey(es.Uid))
                        toShow.Add(es.Uid);
                    uid = es.Uid;
                }
                else //Unknown entities
                {
                    var metaState = (MetaDataComponentState?) es.ComponentChanges.Value?.FirstOrDefault(c => c.NetID == _metaCompNetId).State;
                    if (metaState == null)
                    {
                        throw new InvalidOperationException($"Server sent new entity state for {es.Uid} without metadata component!");
                    }
                    // Logger.Debug($"[{IGameTiming.TickStampStatic}] CREATE {es.Uid} {metaState.PrototypeId}");
                    var newEntity = _entities.CreateEntity(metaState.PrototypeId, es.Uid);
                    toApply.Add(newEntity, (es, null));
                    toInitialize.Add(newEntity);
                    created.Add(newEntity.Uid);
                    uid = newEntity.Uid;
                }
                if(es.Hide)
                    toHide.Add(uid);
            }

            foreach (var es in nextEntStates)
            {
                if (_entities.TryGetEntity(es.Uid, out var entity))
                {
                    if (toApply.TryGetValue(entity, out var state))
                    {
                        toApply[entity] = (state.Item1, es);
                    }
                    else
                    {
                        toApply[entity] = (null, es);
                    }
                }
            }

            // Make sure this is done after all entities have been instantiated.
            foreach (var kvStates in toApply)
            {
                var ent = kvStates.Key;
                var entity = ent;
                HandleEntityState(entity, _entities.EventBus, kvStates.Value.Item1,
                    kvStates.Value.Item2);
            }

            foreach (var id in deletions)
            {
                // Logger.Debug($"[{IGameTiming.TickStampStatic}] DELETE {id}");
                _entities.DeleteEntity(id);
            }

#if EXCEPTION_TOLERANCE
            HashSet<Entity> brokenEnts = new HashSet<Entity>();
#endif

            foreach (var entity in toInitialize)
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
                    Logger.ErrorS("state", $"Server entity threw in Init: uid={entity.Uid}, proto={entity.Prototype}\n{e}");
                    brokenEnts.Add(entity);
                }
#endif
            }

            foreach (var entity in toInitialize)
            {
#if EXCEPTION_TOLERANCE
                if (brokenEnts.Contains(entity))
                    continue;

                try
                {
#endif
                    _entities.StartEntity(entity);
#if EXCEPTION_TOLERANCE
                }
                catch (Exception e)
                {
                    Logger.ErrorS("state", $"Server entity threw in Start: uid={entity.Uid}, proto={entity.Prototype}\n{e}");
                    brokenEnts.Add(entity);
                }
#endif
            }

#if EXCEPTION_TOLERANCE
            foreach (var entity in brokenEnts)
            {
                entity.Delete();
            }
#endif

            foreach (var entityUid in toHide)
            {
                if(_entityManager.HasComponent<MapGridComponent>(entityUid)) continue;

                var xform = _entityManager.GetComponent<TransformComponent>(entityUid);
                _hiddenEntities.Add(entityUid, xform.MapID);
                xform.ChangeMapId(MapId.Nullspace);
            }

            foreach (var entityUid in toShow)
            {
                _entityManager.GetComponent<TransformComponent>(entityUid).ChangeMapId(_hiddenEntities[entityUid]);
                _hiddenEntities.Remove(entityUid);
            }

            return created;
        }

        private void HandleEntityState(IEntity entity, IEventBus bus, EntityState? curState,
            EntityState? nextState)
        {
            var compStateWork = new Dictionary<ushort, (ComponentState? curState, ComponentState? nextState)>();
            var entityUid = entity.Uid;

            if (curState != null)
            {
                foreach (var compChange in curState.ComponentChanges.Span)
                {
                    if (compChange.Deleted)
                    {
                        if (_entityManager.TryGetComponent(entityUid, compChange.NetID, out var comp))
                        {
                            _entityManager.RemoveComponent(entityUid, comp);
                        }
                    }
                    else
                    {
                        //Right now we just assume every state from an unseen entity is added

                        if (_entityManager.HasComponent(entityUid, compChange.NetID))
                            continue;

                        var newComp = (Component) _compFactory.GetComponent(compChange.NetID);
                        newComp.Owner = entity;
                        _entityManager.AddComponent(entity, newComp, true);

                        compStateWork[compChange.NetID] = (compChange.State, null);
                    }
                }

                foreach (var compChange in curState.ComponentChanges.Span)
                {
                    compStateWork[compChange.NetID] = (compChange.State, null);
                }
            }

            if (nextState != null)
            {
                foreach (var compState in nextState.ComponentChanges.Span)
                {
                    if (compStateWork.TryGetValue(compState.NetID, out var state))
                    {
                        compStateWork[compState.NetID] = (state.curState, compState.State);
                    }
                    else
                    {
                        compStateWork[compState.NetID] = (null, compState.State);
                    }
                }
            }

            foreach (var (netId, (cur, next)) in compStateWork)
            {
                if (_entityManager.TryGetComponent(entityUid, (ushort) netId, out var component))
                {
                    try
                    {
                        var handleState = new ComponentHandleState(cur, next);
                        bus.RaiseComponentEvent(component, ref handleState);
                        component.HandleComponentState(cur, next);
                    }
                    catch (Exception e)
                    {
                        var wrapper = new ComponentStateApplyException(
                            $"Failed to apply comp state: entity={component.Owner}, comp={component.Name}", e);
#if EXCEPTION_TOLERANCE
                    _runtimeLog.LogException(wrapper, "Component state apply");
#else
                        throw wrapper;
#endif
                    }
                }
                else
                {
                    // The component can be null here due to interp.
                    // Because the NEXT state will have a new component, but this one doesn't yet.
                    // That's fine though.
                    if (cur == null)
                    {
                        continue;
                    }

                    var eUid = entityUid;
                    var eRegisteredNetUidName = _compFactory.GetRegistration(netId).Name;
                    DebugTools.Assert(
                        $"Component does not exist for state: entUid={eUid}, expectedNetId={netId}, expectedName={eRegisteredNetUidName}");
                }
            }
        }
    }

    public class GameStateAppliedArgs : EventArgs
    {
        public GameState AppliedState { get; }

        public GameStateAppliedArgs(GameState appliedState)
        {
            AppliedState = appliedState;
        }
    }
}
