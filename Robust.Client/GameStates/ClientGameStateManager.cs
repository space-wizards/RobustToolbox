using System.Collections.Generic;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameStates;
using Robust.Shared.GameStates;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network.Messages;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameStates
{
    /// <inheritdoc />
    public class ClientGameStateManager : IClientGameStateManager
    {
        private readonly List<GameState> _stateBuffer = new List<GameState>();
        private GameState _lastFullState;
        private bool _waitingForFull = true;
        private bool _interpEnabled;
        private int _interpRatio;
        private bool _logging;

        [Dependency] private readonly IClientEntityManager _entities;
        [Dependency] private readonly IPlayerManager _players;
        [Dependency] private readonly IClientNetManager _network;
        [Dependency] private readonly IBaseClient _client;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IGameTiming _timing;
        [Dependency] private readonly IConfigurationManager _config;

        /// <inheritdoc />
        public int MinBufferSize => _interpEnabled ? 3 : 2;

        /// <inheritdoc />
        public int TargetBufferSize => MinBufferSize + _interpRatio;

        /// <inheritdoc />
        public void Initialize()
        {
            _network.RegisterNetMessage<MsgState>(MsgState.NAME, HandleStateMessage);
            _network.RegisterNetMessage<MsgStateAck>(MsgStateAck.NAME);
            _client.RunLevelChanged += RunLevelChanged;

            if(!_config.IsCVarRegistered("net.interp"))
                _config.RegisterCVar("net.interp", false, CVar.ARCHIVE, b => _interpEnabled = b);

            if (!_config.IsCVarRegistered("net.interp_ratio"))
                _config.RegisterCVar("net.interp_ratio", 0, CVar.ARCHIVE, i => _interpRatio = i < 0 ? 0 : i);

            if (!_config.IsCVarRegistered("net.logging"))
                _config.RegisterCVar("net.logging", false, CVar.ARCHIVE, b => _logging = b);

            _interpEnabled = _config.GetCVar<bool>("net.interp");

            _interpRatio = _config.GetCVar<int>("net.interp_ratio");
            _interpRatio = _interpRatio < 0 ? 0 : _interpRatio; // min bound, < 0 makes no sense

            _logging = _config.GetCVar<bool>("net.logging");
        }

        /// <inheritdoc />
        public void Reset()
        {
            _stateBuffer.Clear();
            _lastFullState = null;
            _waitingForFull = true;
        }

        private void RunLevelChanged(object sender, RunLevelChangedEventArgs args)
        {
            if (args.NewLevel == ClientRunLevel.Initialize)
            {
                // We JUST left a server or the client started up, Reset everything.
                _stateBuffer.Clear();
            }
        }

        private void HandleStateMessage(MsgState message)
        {
            var state = message.State;

            // we always ack everything we receive, even if it is late
            AckGameState(state.ToSequence);

            // any state from tick 0 is a full state, and needs to be handled different
            if (state.FromSequence == GameTick.Zero)
            {
                // this is a newer full state, so discard the older one.
                if(_lastFullState == null || (_lastFullState != null && _lastFullState.ToSequence < state.ToSequence))
                {
                    _lastFullState = state;

                    if(_logging)
                        Logger.InfoS("net", $"Received Full GameState: to={state.ToSequence}, sz={message.MsgSize}");

                    return;
                }
            }

            // NOTE: DispatchTick will be modifying CurTick, this is NOT thread safe.
            var lastTick = new GameTick(_timing.CurTick.Value - 1);

            if (state.ToSequence <= lastTick && !_waitingForFull) // CurTick isn't set properly when WaitingForFull
            {
                if (_logging)
                    Logger.DebugS("net.state", $"Received Old GameState: cTick={_timing.CurTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={message.MsgSize}, buf={_stateBuffer.Count}");

                return;
            }

            // lets check for a duplicate state now.
            for (var i = 0; i < _stateBuffer.Count; i++)
            {
                var iState = _stateBuffer[i];

                if (state.ToSequence != iState.ToSequence)
                    continue;

                if (iState.Extrapolated)
                {
                    _stateBuffer.RemoveSwap(i); // remove the fake extrapolated state
                    break; // break from the loop, add the new state normally
                }

                if (_logging)
                    Logger.DebugS("net.state", $"Received Dupe GameState: cTick={_timing.CurTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={message.MsgSize}, buf={_stateBuffer.Count}");

                return;
            }

            // this is a good state that we will be using.
            _stateBuffer.Add(state);

            if (_logging)
                Logger.DebugS("net.state", $"Received New GameState: cTick={_timing.CurTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={message.MsgSize}, buf={_stateBuffer.Count}");
        }

        /// <inheritdoc />
        public void ApplyGameState()
        {
            if (CalculateNextStates(_timing.CurTick, out var curState, out var nextState, TargetBufferSize))
            {
                if (_logging)
                    Logger.DebugS("net.state", $"Applying State:  ext={curState.Extrapolated}, cTick={_timing.CurTick}, fSeq={curState.FromSequence}, tSeq={curState.ToSequence}, buf={_stateBuffer.Count}");

                ApplyGameState(curState, nextState);
            }

            if (!_waitingForFull)
            {
                // This will slightly speed up or slow down the client tickrate based on the contents of the buffer.
                // CalcNextState should have just cleaned out any old states, so the buffer contains [t-1(last), t+0(cur), t+1(next), t+2, t+3, ..., t+n]
                // we can use this info to properly time our tickrate so it does not run fast or slow compared to the server.
                _timing.TickTimingAdjustment = (_stateBuffer.Count - (float) TargetBufferSize) * 0.10f;
            }
            else
            {
                _timing.TickTimingAdjustment = 0f;
            }
        }

        private bool CalculateNextStates(GameTick curTick, out GameState curState, out GameState nextState, int targetBufferSize)
        {
            if (_waitingForFull)
            {
                return CalculateFullState(out curState, out nextState, targetBufferSize);
            }
            else // this will be how almost all states are calculated
            {
                return CalculateDeltaState(curTick, out curState, out nextState);
            }
        }

        private bool CalculateFullState(out GameState curState, out GameState nextState, int targetBufferSize)
        {
            if (_lastFullState != null)
            {
                if (_logging)
                    Logger.DebugS("net", $"Resync CurTick to: {_lastFullState.ToSequence}");

                var curTick = _timing.CurTick = _lastFullState.ToSequence;

                if (_interpEnabled)
                {
                    // look for the next state
                    var lastTick = new GameTick(curTick.Value - 1);
                    var nextTick = new GameTick(curTick.Value + 1);
                    nextState = null;

                    for (var i = 0; i < _stateBuffer.Count; i++)
                    {
                        var state = _stateBuffer[i];
                        if (state.ToSequence == nextTick)
                        {
                            nextState = state;
                        }
                        else if (state.ToSequence < lastTick) // remove any old states we find to keep the buffer clean
                        {
                            _stateBuffer.RemoveSwap(i);
                            i--;
                        }
                    }

                    // we let the buffer fill up before starting to tick
                    if (nextState != null && _stateBuffer.Count >= targetBufferSize)
                    {
                        curState = _lastFullState;
                        _waitingForFull = false;
                        return true;
                    }
                }
                else if (_stateBuffer.Count >= targetBufferSize)
                {
                    curState = _lastFullState;
                    nextState = default;
                    _waitingForFull = false;
                    return true;
                }
            }

            if (_logging)
                Logger.DebugS("net", $"Have FullState, filling buffer... ({_stateBuffer.Count}/{targetBufferSize})");

            // waiting for full state or buffer to fill
            curState = default;
            nextState = default;
            return false;
        }

        private bool CalculateDeltaState(GameTick curTick, out GameState curState, out GameState nextState)
        {
            var lastTick = new GameTick(curTick.Value - 1);
            var nextTick = new GameTick(curTick.Value + 1);

            curState = null;
            nextState = null;

            for (var i = 0; i < _stateBuffer.Count; i++)
            {
                var state = _stateBuffer[i];

                // remember there are no duplicate ToSequence states in the list.
                if (state.ToSequence == curTick)
                {
                    curState = state;
                }
                else if (_interpEnabled && state.ToSequence == nextTick)
                {
                    nextState = state;
                }
                else if (state.ToSequence < lastTick) // remove any old states we find to keep the buffer clean
                {
                    _stateBuffer.RemoveSwap(i);
                    i--;
                }
            }

            // we found both the states to interpolate between, this should almost always be true.
            if ((_interpEnabled && curState != null) || (!_interpEnabled && curState != null && nextState != null))
                return true;

            if (curState == null)
            {
                curState = ExtrapolateState(lastTick, curTick);
            }

            if (nextState == null && _interpEnabled)
            {
                nextState = ExtrapolateState(curTick, nextTick);
            }

            return true;
        }

        /// <summary>
        ///     Generates a completely fake GameState.
        /// </summary>
        private static GameState ExtrapolateState(GameTick fromSequence, GameTick toSequence)
        {
           var state = new GameState(fromSequence, toSequence, null, null, null, null);
           state.Extrapolated = true;
           return state;
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
        }
    }
}
