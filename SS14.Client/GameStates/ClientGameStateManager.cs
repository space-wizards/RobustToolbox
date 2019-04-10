using System.Collections.Generic;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameStates;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network.Messages;
using SS14.Client.Player;
using SS14.Shared.Configuration;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.Timing;
using SS14.Shared.Utility;

namespace SS14.Client.GameStates
{
    public class ClientGameStateManager : IClientGameStateManager
    {
        private readonly List<GameState> _stateBuffer = new List<GameState>();
        private GameState _lastFullState;
        private bool _waitingForFull = true;

        [Dependency]
        private readonly IClientEntityManager _entities;
        [Dependency]
        private readonly IPlayerManager _players;
        [Dependency]
        private readonly IClientNetManager _network;
        [Dependency]
        private readonly IBaseClient _client;
        [Dependency]
        private readonly IMapManager _mapManager;
        [Dependency]
        private readonly IGameTiming _timing;
        [Dependency]
        private readonly IConfigurationManager _config;

        /// <inheritdoc />
        public void Initialize()
        {
            _network.RegisterNetMessage<MsgState>(MsgState.NAME, HandleStateMessage);
            _network.RegisterNetMessage<MsgStateAck>(MsgStateAck.NAME);
            _client.RunLevelChanged += RunLevelChanged;

            if(!_config.IsCVarRegistered("net.state_buffer"))
                _config.RegisterCVar("net.state_buffer", 1, CVar.ARCHIVE);
        }

        /// <inheritdoc />
        public void Shutdown()
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
                    Logger.InfoS("net", $"Received Full GameState: to={state.ToSequence}, sz={message.MsgSize}");
                    return;
                }
            }

            // NOTE: DispatchTick will be modifying CurTick, this is NOT thread safe.
            var lastTick = new GameTick(_timing.CurTick.Value - 1);

            if (state.ToSequence <= lastTick && !_waitingForFull) // CurTick isn't set properly when WaitingForFull
            {
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

                Logger.DebugS("net.state", $"Received Dupe GameState: cTick={_timing.CurTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={message.MsgSize}, buf={_stateBuffer.Count}");
                return;
            }

            // this is a good state that we will be using.
            _stateBuffer.Add(state);
            Logger.DebugS("net.state", $"Received New GameState: cTick={_timing.CurTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={message.MsgSize}, buf={_stateBuffer.Count}");
        }

        public void ApplyGameState()
        {
            var bufSz = _config.GetCVar<int>("net.state_buffer");
            bufSz = bufSz < 0 ? 0 : bufSz; // min bound, < 0 makes no sense
            var targetBufferSize = 3 + bufSz; // absolute minimum is 3 states in buffer for the system to work (last, cur, next)
            
            //TODO: completely skip this tick and freeze the sim if false
            if (CalculateNextStates(_timing.CurTick, out var curState, out var nextState, targetBufferSize))
            {
                Logger.DebugS("net.state", $"Applying State:  ext={curState.Extrapolated}, cTick={_timing.CurTick}, fSeq={curState.FromSequence}, tSeq={curState.ToSequence}, buf={_stateBuffer.Count}");
                ApplyGameState(curState, nextState);
            }

            if (!_waitingForFull)
            {
                // This will slightly speed up or slow down the client tickrate based on the contents of the buffer.
                // CalcNextState should have just cleaned out any old states, so the buffer contains [t-1(last), t+0(cur), t+1(next), t+2, t+3, ..., t+n]
                // we can use this info to properly time our tickrate so it does not run fast or slow compared to the server.
                _timing.TickTimingAdjustment = (_stateBuffer.Count - (float) targetBufferSize) * 0.10f;
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
                if (_lastFullState != null)
                {
                    Logger.DebugS("net", $"Resync CurTick to: {_lastFullState.ToSequence}");
                    curTick = _timing.CurTick = _lastFullState.ToSequence;

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

                // We just have to wait...
                curState = default;
                nextState = default;
                return false;
            }
            else // this will be how almost all states are calculated
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
                    else if (state.ToSequence == nextTick)
                    {
                        nextState = state;
                    }
                    else if(state.ToSequence < lastTick) // remove any old states we find to keep the buffer clean
                    {
                        _stateBuffer.RemoveSwap(i);
                        i--;
                    }
                }

                // we found both the states to lerp between, this should be true almost always.
                if (curState != null && nextState != null)
                    return true;

                if (curState == null)
                {
                    curState = ExtrapolateState(lastTick, curTick);
                    _stateBuffer.Add(curState);
                }

                if (nextState == null)
                {
                    nextState = ExtrapolateState(curTick, nextTick);
                    _stateBuffer.Add(nextState);
                }

                return true;
            }
        }

        /// <summary>
        ///     Generates a completely fake GameState.
        /// </summary>
        private GameState ExtrapolateState(GameTick fromSequence, GameTick toSequence)
        {
            //TODO: Make me work!
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
            _entities.ApplyEntityStates(curState.EntityStates, curState.EntityDeletions, nextState.EntityStates);
            _players.ApplyPlayerStates(curState.PlayerStates);
            _mapManager.ApplyGameStatePost(curState.MapData);
        }
    }
}
