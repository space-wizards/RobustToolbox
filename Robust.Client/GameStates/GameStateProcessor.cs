using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameStates
{
    /// <inheritdoc />
    internal class GameStateProcessor : IGameStateProcessor
    {
        private readonly IGameTiming _timing;

        private readonly List<GameState> _stateBuffer = new List<GameState>();
        private GameState _lastFullState;
        private bool _waitingForFull = true;
        private int _interpRatio;
        private GameTick _lastProcessedRealState;

        /// <inheritdoc />
        public int MinBufferSize => Interpolation ? 3 : 2;

        /// <inheritdoc />
        public int TargetBufferSize => MinBufferSize + InterpRatio;

        /// <inheritdoc />
        public int CurrentBufferSize => _stateBuffer.Count;

        /// <inheritdoc />
        public bool Interpolation { get; set; }

        /// <inheritdoc />
        public int InterpRatio
        {
            get => _interpRatio;
            set => _interpRatio = value < 0 ? 0 : value;
        }

        /// <inheritdoc />
        public bool Extrapolation { get; set; }

        /// <inheritdoc />
        public bool Logging { get; set; }

        /// <summary>
        ///     Constructs a new instance of <see cref="GameStateProcessor"/>.
        /// </summary>
        /// <param name="timing">Timing information of the current state.</param>
        public GameStateProcessor(IGameTiming timing)
        {
            _timing = timing;
        }

        /// <inheritdoc />
        public void AddNewState(GameState state)
        {
            // any state from tick 0 is a full state, and needs to be handled different
            if (state.FromSequence == GameTick.Zero)
            {
                // this is a newer full state, so discard the older one.
                if (_lastFullState == null || (_lastFullState != null && _lastFullState.ToSequence < state.ToSequence))
                {
                    _lastFullState = state;

                    if (Logging)
                        Logger.InfoS("net", $"Received Full GameState: to={state.ToSequence}, sz={state.PayloadSize}");

                    return;
                }
            }

            // NOTE: DispatchTick will be modifying CurTick, this is NOT thread safe.
            var lastTick = new GameTick(_timing.CurTick.Value - 1);

            if (state.ToSequence <= lastTick && !_waitingForFull) // CurTick isn't set properly when WaitingForFull
            {
                if (Logging)
                    Logger.DebugS("net.state", $"Received Old GameState: cTick={_timing.CurTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={state.PayloadSize}, buf={_stateBuffer.Count}");

                return;
            }

            // lets check for a duplicate state now.
            for (var i = 0; i < _stateBuffer.Count; i++)
            {
                var iState = _stateBuffer[i];

                if (state.ToSequence != iState.ToSequence)
                    continue;

                if (Logging)
                    Logger.DebugS("net.state", $"Received Dupe GameState: cTick={_timing.CurTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={state.PayloadSize}, buf={_stateBuffer.Count}");

                return;
            }

            // this is a good state that we will be using.
            _stateBuffer.Add(state);

            if (Logging)
                Logger.DebugS("net.state", $"Received New GameState: cTick={_timing.CurTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={state.PayloadSize}, buf={_stateBuffer.Count}");
        }

        /// <inheritdoc />
        public bool ProcessTickStates(GameTick curTick, out GameState curState, out GameState nextState)
        {
            bool applyNextState;
            if (_waitingForFull)
            {
                applyNextState = CalculateFullState(out curState, out nextState, TargetBufferSize);
            }
            else // this will be how almost all states are calculated
            {
                applyNextState = CalculateDeltaState(curTick, out curState, out nextState);
            }

            if (applyNextState && !curState.Extrapolated)
                _lastProcessedRealState = curState.ToSequence;
            
            if (!_waitingForFull)
            {
                if (!applyNextState)
                    _timing.CurTick = _lastProcessedRealState;

                // This will slightly speed up or slow down the client tickrate based on the contents of the buffer.
                // CalcNextState should have just cleaned out any old states, so the buffer contains [t-1(last), t+0(cur), t+1(next), t+2, t+3, ..., t+n]
                // we can use this info to properly time our tickrate so it does not run fast or slow compared to the server.
                _timing.TickTimingAdjustment = (_stateBuffer.Count - (float)TargetBufferSize) * 0.10f;
            }
            else
            {
                _timing.TickTimingAdjustment = 0f;
            }

            if (Logging && applyNextState)
                Logger.DebugS("net.state", $"Applying State:  ext={curState.Extrapolated}, cTick={_timing.CurTick}, fSeq={curState.FromSequence}, tSeq={curState.ToSequence}, buf={_stateBuffer.Count}");

            return applyNextState;
        }

        private bool CalculateFullState(out GameState curState, out GameState nextState, int targetBufferSize)
        {
            if (_lastFullState != null)
            {
                if (Logging)
                    Logger.DebugS("net", $"Resync CurTick to: {_lastFullState.ToSequence}");

                var curTick = _timing.CurTick = _lastFullState.ToSequence;

                if (Interpolation)
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

            if (Logging)
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
                else if (Interpolation && state.ToSequence == nextTick)
                {
                    nextState = state;
                }
                else if (state.ToSequence < lastTick) // remove any old states we find to keep the buffer clean
                {
                    _stateBuffer.RemoveSwap(i);
                    i--;
                }
            }

            // we won't extrapolate, and curState was not found.
            if (!Extrapolation && curState == null)
                return false;

            // we found both the states to interpolate between, this should almost always be true.
            if (Interpolation && curState != null)
                return true;

            if (!Interpolation && curState != null && nextState != null)
                return true;

            if (curState == null)
            {
                curState = ExtrapolateState(lastTick, curTick);
            }

            if (nextState == null && Interpolation)
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

        /// <inheritdoc />
        public void Reset()
        {
            _stateBuffer.Clear();
            _lastFullState = null;
            _waitingForFull = true;
        }
    }
}
