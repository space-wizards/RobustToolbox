using System;
using System.Collections.Generic;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameStates;
using Robust.Client.Interfaces.Input;
using Robust.Shared.GameStates;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Math = CannyFastMath.Math;
using MathF = CannyFastMath.MathF;

namespace Robust.Client.GameStates
{
    /// <inheritdoc />
    public class ClientGameStateManager : IClientGameStateManager
    {
        private GameStateProcessor _processor = default!;

        private uint _nextInputCmdSeq = 1;
        private readonly Queue<FullInputCmdMessage> _pendingInputs = new Queue<FullInputCmdMessage>();

        private readonly Queue<(uint sequence, GameTick sourceTick, EntitySystemMessage msg, object sessionMsg)>
            _pendingSystemMessages
                = new Queue<(uint, GameTick, EntitySystemMessage, object)>();

        [Dependency] private readonly IClientEntityManager _entities = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IClientNetManager _network = default!;
        [Dependency] private readonly IBaseClient _client = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IConfigurationManager _config = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IComponentManager _componentManager = default!;

        /// <inheritdoc />
        public int MinBufferSize => _processor.MinBufferSize;

        /// <inheritdoc />
        public int TargetBufferSize => _processor.TargetBufferSize;

        /// <inheritdoc />
        public int CurrentBufferSize => _processor.CalculateBufferSize(CurServerTick);

        public bool Predicting { get; private set; }

        public int PredictSize { get; private set; }

        private uint _lastProcessedSeq;
        private GameTick _lastProcessedTick = GameTick.Zero;

        public GameTick CurServerTick => _lastProcessedTick;

        /// <inheritdoc />
        public event Action<GameStateAppliedArgs>? GameStateApplied;

        /// <inheritdoc />
        public void Initialize()
        {
            _processor = new GameStateProcessor(_timing);

            _network.RegisterNetMessage<MsgState>(MsgState.NAME, HandleStateMessage);
            _network.RegisterNetMessage<MsgStateAck>(MsgStateAck.NAME);
            _client.RunLevelChanged += RunLevelChanged;

            _config.RegisterCVar("net.interp", false, CVar.ARCHIVE, b => _processor.Interpolation = b);
            _config.RegisterCVar("net.interp_ratio", 0, CVar.ARCHIVE, i => _processor.InterpRatio = i);
            _config.RegisterCVar("net.logging", false, CVar.ARCHIVE, b => _processor.Logging = b);
            _config.RegisterCVar("net.predict", true, CVar.ARCHIVE, b => Predicting = b);
            _config.RegisterCVar("net.predict_size", 1, CVar.ARCHIVE, i => PredictSize = i);

            _processor.Interpolation = _config.GetCVar<bool>("net.interp");
            _processor.InterpRatio = _config.GetCVar<int>("net.interp_ratio");
            _processor.Logging = _config.GetCVar<bool>("net.logging");
            Predicting = _config.GetCVar<bool>("net.predict");
            PredictSize = _config.GetCVar<int>("net.predict_size");
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
            //Logger.DebugS("State",
            //    $"CL> SENT tick={_timing.CurTick}, seq={_nextInputCmdSeq}, func={boundFunc.FunctionName}, state={message.State}");
            _nextInputCmdSeq++;
        }

        public uint SystemMessageDispatched<T>(T message) where T : EntitySystemMessage
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
            _timing.CurTick = _lastProcessedTick + 1;

            if (!_processor.ProcessTickStates(_timing.CurTick, out var curState, out var nextState))
            {
                return;
            }

            // TODO: If Predicting gets disabled *while* the world state is dirty from a prediction,
            // this won't run meaning it could potentially get stuck dirty.
            if (Predicting)
            {
                // Disable IsFirstTimePredicted while re-running HandleComponentState here.
                // Helps with debugging.
                using var resetArea = _timing.StartPastPredictionArea();

                ResetPredictedEntities(_timing.CurTick);
            }

            // Store last tick we got from the GameStateProcessor.
            _lastProcessedTick = _timing.CurTick;

            // apply current state
            var createdEntities = ApplyGameState(curState, nextState);

            MergeImplicitData(createdEntities);

            var inputMan = IoCManager.Resolve<IInputManager>();
            var input = EntitySystem.Get<InputSystem>();

            if (_lastProcessedSeq < curState.LastProcessedInput)
            {
                //Logger.DebugS("State", $"SV> RCV  tick={_timing.CurTick}, seq={_lastProcessedSeq}");
                _lastProcessedSeq = curState.LastProcessedInput;
            }

            // remove old pending inputs
            while (_pendingInputs.Count > 0 && _pendingInputs.Peek().InputSequence <= _lastProcessedSeq)
            {
                var inCmd = _pendingInputs.Dequeue();

                inputMan.NetworkBindMap.TryGetKeyFunction(inCmd.InputFunctionId, out var boundFunc);
                //Logger.DebugS("State",
                //    $"SV>     seq={inCmd.InputSequence}, func={boundFunc.FunctionName}, state={inCmd.State}");
            }

            while (_pendingSystemMessages.Count > 0 && _pendingSystemMessages.Peek().sequence <= _lastProcessedSeq)
            {
                _pendingSystemMessages.Dequeue();
            }

            DebugTools.Assert(_timing.InSimulation);

            if (!Predicting) return;

            using var _ = _timing.StartPastPredictionArea();

            /*
            if (_pendingInputs.Count > 0)
            {
                Logger.DebugS("State", "CL> Predicted:");
            }
            */

            var pendingInputEnumerator = _pendingInputs.GetEnumerator();
            var pendingMessagesEnumerator = _pendingSystemMessages.GetEnumerator();
            var hasPendingInput = pendingInputEnumerator.MoveNext();
            var hasPendingMessage = pendingMessagesEnumerator.MoveNext();

            var ping = _network.ServerChannel!.Ping / 1000f; // seconds.
            var targetTick = _timing.CurTick.Value + _processor.TargetBufferSize +
                             (int) Math.Ceiling(_timing.TickRate * ping) + PredictSize;

            //Logger.DebugS("State", $"Predicting from {_lastProcessedTick} to {targetTick}");

            for (var t = _lastProcessedTick.Value; t <= targetTick; t++)
            {
                var tick = new GameTick(t);
                _timing.CurTick = tick;

                while (hasPendingInput && pendingInputEnumerator.Current.Tick <= tick)
                {
                    var inputCmd = pendingInputEnumerator.Current;

                    inputMan.NetworkBindMap.TryGetKeyFunction(inputCmd.InputFunctionId, out var boundFunc);
                    /*
                    Logger.DebugS("State",
                        $"    seq={inputCmd.InputSequence}, dTick={tick}, func={boundFunc.FunctionName}, " +
                        $"state={inputCmd.State}");
                        */

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
                    // Don't run EntitySystemManager.Update if this is the target tick,
                    // because the rest of the main loop will call into it with the target tick later,
                    // and it won't be a past prediction.
                    _entitySystemManager.Update((float) _timing.TickPeriod.TotalSeconds);
                    ((IEntityEventBus) _entities.EventBus).ProcessEventQueue();
                }
            }
        }

        private void ResetPredictedEntities(GameTick curTick)
        {
            foreach (var entity in _entities.GetEntities())
            {
                // TODO: 99% there's an off-by-one here.
                if (entity.Uid.IsClientSide() || entity.LastModifiedTick < curTick)
                {
                    continue;
                }

                // Logger.DebugS("State", $"Entity {entity.Uid} was made dirty.");

                var last = _processor.GetLastServerStates(entity.Uid);

                // TODO: handle component deletions/creations.
                foreach (var comp in _componentManager.GetNetComponents(entity.Uid))
                {
                    DebugTools.AssertNotNull(comp.NetID);

                    if (comp.LastModifiedTick < curTick || !last.TryGetValue(comp.NetID!.Value, out var compState))
                    {
                        continue;
                    }

                    // Logger.DebugS("State", $"  And also its component {comp.Name}");
                    // TODO: Handle interpolation.
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

            foreach (var createdEntity in createdEntities)
            {
                var compData = new Dictionary<uint, ComponentState>();
                outputData.Add(createdEntity, compData);

                foreach (var component in _componentManager.GetNetComponents(createdEntity))
                {
                    var state = component.GetComponentState();

                    if (state.GetType() == typeof(ComponentState))
                    {
                        continue;
                    }

                    compData.Add(state.NetID, state);
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
            _mapManager.ApplyGameStatePre(curState.MapData);
            var createdEntities = _entities.ApplyEntityStates(curState.EntityStates, curState.EntityDeletions,
                nextState?.EntityStates);
            _players.ApplyPlayerStates(curState.PlayerStates);
            _mapManager.ApplyGameStatePost(curState.MapData);

            GameStateApplied?.Invoke(new GameStateAppliedArgs(curState));
            return createdEntities;
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
