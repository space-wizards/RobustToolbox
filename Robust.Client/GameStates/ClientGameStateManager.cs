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
using Robust.Shared.Input;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameStates
{
    /// <inheritdoc />
    public class ClientGameStateManager : IClientGameStateManager
    {
        private GameStateProcessor _processor;

        private uint _nextInputCmdSeq = 1;
        private Queue<FullInputCmdMessage> _pendingInputs = new Queue<FullInputCmdMessage>();

#pragma warning disable 649
        [Dependency] private readonly IClientEntityManager _entities;
        [Dependency] private readonly IPlayerManager _players;
        [Dependency] private readonly IClientNetManager _network;
        [Dependency] private readonly IBaseClient _client;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IGameTiming _timing;
        [Dependency] private readonly IConfigurationManager _config;
#pragma warning restore 649

        /// <inheritdoc />
        public int MinBufferSize => _processor.MinBufferSize;

        /// <inheritdoc />
        public int TargetBufferSize => _processor.TargetBufferSize;

        /// <inheritdoc />
        public int CurrentBufferSize => _processor.CurrentBufferSize;

        /// <inheritdoc />
        public event Action<GameStateAppliedArgs> GameStateApplied;

        /// <inheritdoc />
        public void Initialize()
        {
            _processor = new GameStateProcessor(_timing);

            _network.RegisterNetMessage<MsgState>(MsgState.NAME, HandleStateMessage);
            _network.RegisterNetMessage<MsgStateAck>(MsgStateAck.NAME);
            _client.RunLevelChanged += RunLevelChanged;

            if(!_config.IsCVarRegistered("net.interp"))
                _config.RegisterCVar("net.interp", false, CVar.ARCHIVE, b => _processor.Interpolation = b);

            if (!_config.IsCVarRegistered("net.interp_ratio"))
                _config.RegisterCVar("net.interp_ratio", 0, CVar.ARCHIVE, i => _processor.InterpRatio = i);

            if (!_config.IsCVarRegistered("net.logging"))
                _config.RegisterCVar("net.logging", false, CVar.ARCHIVE, b => _processor.Logging = b);

            _processor.Interpolation = _config.GetCVar<bool>("net.interp");
            _processor.InterpRatio = _config.GetCVar<int>("net.interp_ratio");
            _processor.Logging = _config.GetCVar<bool>("net.logging");
        }

        /// <inheritdoc />
        public void Reset()
        {
            _processor.Reset();
        }

        private void RunLevelChanged(object sender, RunLevelChangedEventArgs args)
        {
            if (args.NewLevel == ClientRunLevel.Initialize)
            {
                // We JUST left a server or the client started up, Reset everything.
                Reset();
            }
        }

        public void InputCommandDispatched(FullInputCmdMessage message)
        {
            message.InputSequence = _nextInputCmdSeq;
            _pendingInputs.Enqueue(message);

            var inputMan = IoCManager.Resolve<IInputManager>();
            inputMan.NetworkBindMap.TryGetKeyFunction(message.InputFunctionId, out var boundFunc);
            Logger.DebugS("State", $"CL> SENT tick={_timing.CurTick}, seq={_nextInputCmdSeq}, func={boundFunc.FunctionName}, state={message.State}");
            _nextInputCmdSeq++;
        }

        private void HandleStateMessage(MsgState message)
        {
            var state = message.State;

            _processor.AddNewState(state);

            // we always ack everything we receive, even if it is late
            AckGameState(state.ToSequence);
        }

        private uint lastProcessedSeq;
        GameTick lastProcessedTick = GameTick.Zero;

        /// <inheritdoc />
        public void ApplyGameState()
        {
            if (!_processor.ProcessTickStates(_timing.CurTick, out var curState, out var nextState))
                return;

            // apply current state
            ApplyGameState(curState, nextState);

            var sysMan = IoCManager.Resolve<IEntitySystemManager>();
            var inputMan = IoCManager.Resolve<IInputManager>();
            var input = sysMan.GetEntitySystem<InputSystem>();

            if (lastProcessedSeq < curState.LastProcessedInput)
            {
                Logger.DebugS("State", $"SV> RCV  tick={_timing.CurTick}, seq={lastProcessedSeq}");
                lastProcessedSeq = curState.LastProcessedInput;
            }

            // remove old pending inputs
            while (_pendingInputs.Count > 0 && _pendingInputs.Peek().InputSequence <= lastProcessedSeq)
            {
                var inCmd = _pendingInputs.Dequeue();
                lastProcessedTick = inCmd.Tick;

                inputMan.NetworkBindMap.TryGetKeyFunction(inCmd.InputFunctionId, out var boundFunc);
                Logger.DebugS("State", $"SV>     seq={inCmd.InputSequence}, func={boundFunc.FunctionName}, state={inCmd.State}");
            }

            DebugTools.Assert(_timing.InSimulation);
            GameTick realCurrentTick = _timing.CurTick;

            if(_pendingInputs.Count > 0)
                Logger.DebugS("State", $"CL> Predicted:");

            //Replay pending inputs
            foreach (var inputCmd in _pendingInputs)
            {
                //Queue is assumed to be in chronological order
                DebugTools.Assert(inputCmd.Tick.Value >= lastProcessedTick.Value);
                var deltaTick = new GameTick(inputCmd.Tick.Value - lastProcessedTick.Value);

                inputMan.NetworkBindMap.TryGetKeyFunction(inputCmd.InputFunctionId, out var boundFunc);
                Logger.DebugS("State", $"    seq={inputCmd.InputSequence}, dTick={deltaTick}, func={boundFunc.FunctionName}, state={inputCmd.State}");

                _timing.CurTick = new GameTick(realCurrentTick.Value + deltaTick.Value);
                input.PredictInputCommand(inputCmd);
            }

            _timing.CurTick = realCurrentTick;
        }

        private void AckGameState(GameTick sequence)
        {
            var msg = _network.CreateNetMessage<MsgStateAck>();
            msg.Sequence = sequence;
            _network.ClientSendMessage(msg);
        }

        private void ApplyGameState(GameState curState, GameState nextState)
        {
            _mapManager.ApplyGameStatePre(curState.MapData);
            _entities.ApplyEntityStates(curState.EntityStates, curState.EntityDeletions, nextState?.EntityStates);
            _players.ApplyPlayerStates(curState.PlayerStates);
            _mapManager.ApplyGameStatePost(curState.MapData);

            GameStateApplied?.Invoke(new GameStateAppliedArgs(curState));
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
