using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Client.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Network.Messages;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameStates
{
    /// <inheritdoc />
    internal sealed class GameStateProcessor : IGameStateProcessor
    {
        private readonly IClientGameTiming _timing;

        private readonly List<GameState> _stateBuffer = new();

        private readonly Dictionary<GameTick, List<EntityUid>> _pvsDetachMessages = new();

        public GameState? LastFullState { get; private set; }
        public bool WaitingForFull => LastFullStateRequested.HasValue;
        public GameTick? LastFullStateRequested
        {
            get => _lastFullStateRequested;
            set
            {
                _lastFullStateRequested = value;
                LastFullState = null;
            }
        }

        public GameTick? _lastFullStateRequested = GameTick.Zero;

        private int _bufferSize;

        /// <summary>
        /// This dictionary stores the full most recently received server state of any entity. This is used whenever predicted entities get reset.
        /// </summary>
        internal readonly Dictionary<EntityUid, Dictionary<ushort, ComponentState>> _lastStateFullRep
            = new();

        /// <inheritdoc />
        public int MinBufferSize => Interpolation ? 2 : 1;

        /// <inheritdoc />
        public int TargetBufferSize => MinBufferSize + BufferSize;

        /// <inheritdoc />
        public bool Interpolation { get; set; }

        /// <inheritdoc />
        public int BufferSize
        {
            get => _bufferSize;
            set => _bufferSize = value < 0 ? 0 : value;
        }

        /// <inheritdoc />
        public bool Logging { get; set; }

        /// <summary>
        ///     Constructs a new instance of <see cref="GameStateProcessor"/>.
        /// </summary>
        /// <param name="timing">Timing information of the current state.</param>
        public GameStateProcessor(IClientGameTiming timing)
        {
            _timing = timing;
        }

        /// <inheritdoc />
        public bool AddNewState(GameState state)
        {
            // Check for old states.
            if (state.ToSequence <= _timing.LastRealTick)
            {
                if (Logging)
                    Logger.DebugS("net.state", $"Received Old GameState: lastRealTick={_timing.LastRealTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={state.PayloadSize}, buf={_stateBuffer.Count}");

                return false;
            }

            // Check for a duplicate states.
            foreach (var bufferState in _stateBuffer)
            {
                if (state.ToSequence != bufferState.ToSequence)
                    continue;

                if (Logging)
                    Logger.DebugS("net.state", $"Received Dupe GameState: lastRealTick={_timing.LastRealTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={state.PayloadSize}, buf={_stateBuffer.Count}");

                return false;
            }
            
            // Are we expecting a full state?
            if (!WaitingForFull)
            {
                // This is a good state that we will be using.
                _stateBuffer.Add(state);
                if (Logging)
                    Logger.DebugS("net.state", $"Received New GameState: lastRealTick={_timing.LastRealTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={state.PayloadSize}, buf={_stateBuffer.Count}");
                return true;
            }

            if (LastFullState == null && state.FromSequence == GameTick.Zero && state.ToSequence >= LastFullStateRequested!.Value)
            {
                LastFullState = state;

                if (Logging)
                    Logger.InfoS("net", $"Received Full GameState: to={state.ToSequence}, sz={state.PayloadSize}");

                return true;
            }

            if (LastFullState != null && state.ToSequence <= LastFullState.ToSequence)
            {
                if (Logging)
                    Logger.InfoS("net", $"While waiting for full, received late GameState with lower to={state.ToSequence} than the last full state={LastFullState.ToSequence}");

                return false;
            }

            _stateBuffer.Add(state);
            return true;
        }

        /// <summary>
        ///     Attempts to get the current and next states to apply.
        /// </summary>
        /// <remarks>
        ///     If the processor is not currently waiting for a full state, the states to apply depends on <see
        ///     cref="IGameTiming.LastProcessedTick"/>.
        /// </remarks>
        /// <returns>Returns true if the states should be applied.</returns>
        public bool TryGetServerState([NotNullWhen(true)] out GameState? curState, out GameState? nextState)
        {
            var applyNextState = WaitingForFull
                ? TryGetFullState(out curState, out nextState)
                : TryGetDeltaState(out curState, out nextState);

            if (curState != null)
            {
                DebugTools.Assert(curState.FromSequence <= curState.ToSequence,
                    "Tried to apply a non-extrapolated state that has too high of a FromSequence!");

                if (Logging)
                    Logger.DebugS("net.state", $"Applying State:  cTick={_timing.LastProcessedTick}, fSeq={curState.FromSequence}, tSeq={curState.ToSequence}, buf={_stateBuffer.Count}");
            }

            return applyNextState;
        }

        public void UpdateFullRep(GameState state)
        {
            // Note: the most recently received server state currently doesn't include pvs-leave messages (detaching
            // transform to null-space). This is because a client should never predict an entity being moved back from
            // null-space, so there should be no need to reset it back there.

            if (state.FromSequence == GameTick.Zero)
            {
                // Full state.
                _lastStateFullRep.Clear();
            }
            else
            {
                foreach (var deletion in state.EntityDeletions.Span)
                {
                    _lastStateFullRep.Remove(deletion);
                }
            }

            foreach (var entityState in state.EntityStates.Span)
            {
                if (!_lastStateFullRep.TryGetValue(entityState.Uid, out var compData))
                {
                    compData = new Dictionary<ushort, ComponentState>();
                    _lastStateFullRep.Add(entityState.Uid, compData);
                }

                foreach (var change in entityState.ComponentChanges.Span)
                {
                    compData[change.NetID] = change.State;
                }

                if (entityState.NetComponents == null)
                    continue;

                foreach (var key in compData.Keys)
                {
                    if (!entityState.NetComponents.Contains(key))
                        compData.Remove(key);
                }
            }
        }

        private bool TryGetFullState([NotNullWhen(true)] out GameState? curState, out GameState? nextState)
        {
            nextState = null;
            curState = null;

            if (LastFullState == null)
                return false;

            // remove any old states we find to keep the buffer clean
            // also look for the next state if we are interpolating.
            var nextTick = LastFullState.ToSequence + 1;
            for (var i = 0; i < _stateBuffer.Count; i++)
            {
                var state = _stateBuffer[i];

                if (state.ToSequence < LastFullState.ToSequence) 
                {
                    _stateBuffer.RemoveSwap(i);
                    i--;
                }
                else if (Interpolation && state.ToSequence == nextTick)
                {
                    nextState = state;
                }
            }

            // we let the buffer fill up before starting to tick
            if (_stateBuffer.Count >= TargetBufferSize)
            {
                if (Logging)
                    Logger.DebugS("net", $"Resync CurTick to: {LastFullState.ToSequence}");

                curState = LastFullState;
                return true;
            }

            // waiting for buffer to fill
            if (Logging)
                Logger.DebugS("net", $"Have FullState, filling buffer... ({_stateBuffer.Count}/{TargetBufferSize})");
            
            return false;
        }

        internal void AddLeavePvsMessage(MsgStateLeavePvs message)
        {
            // Late message may still need to be processed,
            DebugTools.Assert(message.Entities.Count > 0);
            _pvsDetachMessages.TryAdd(message.Tick, message.Entities);
        }

        public List<(GameTick Tick, List<EntityUid> Entities)> GetEntitiesToDetach(GameTick toTick, int budget)
        {
            var result = new List<(GameTick Tick, List<EntityUid> Entities)>();
            foreach (var (tick, entities) in _pvsDetachMessages)
            {
                if (tick > toTick)
                    continue;

                if (budget >= entities.Count)
                {
                    budget -= entities.Count;
                    _pvsDetachMessages.Remove(tick);
                    result.Add((tick, entities));
                    continue;
                }

                var index = entities.Count - budget;
                result.Add((tick, entities.GetRange(index, budget)));
                entities.RemoveRange(index, budget);
                break;
            }
            return result;
        }

        private bool TryGetDeltaState(out GameState? curState, out GameState? nextState)
        {
            curState = null;
            nextState = null;

            var targetCurTick = _timing.LastProcessedTick + 1;
            var targetNextTick = _timing.LastProcessedTick + 2;

            GameTick? futureStateLowestFromSeq = null;

            for (var i = 0; i < _stateBuffer.Count; i++)
            {
                var state = _stateBuffer[i];

                // remember there are no duplicate ToSequence states in the list.
                if (state.ToSequence == targetCurTick && state.FromSequence <= _timing.LastRealTick)
                {
                    curState = state;
                    continue;
                }

                if (Interpolation && state.ToSequence == targetNextTick)
                    nextState = state;

                if (state.ToSequence > targetCurTick && (futureStateLowestFromSeq == null || futureStateLowestFromSeq.Value > state.FromSequence))
                {
                    futureStateLowestFromSeq = state.FromSequence;
                    continue;
                }

                // remove any old states we find to keep the buffer clean
                if (state.ToSequence <= _timing.LastRealTick) 
                {
                    _stateBuffer.RemoveSwap(i);
                    i--;
                }
            }

            // Even if we can't find current state, maybe we have a future state?
            return curState != null || (futureStateLowestFromSeq != null && futureStateLowestFromSeq <= _timing.LastRealTick);
        }

        /// <inheritdoc />
        public void Reset()
        {
            _stateBuffer.Clear();
            LastFullState = null;
            LastFullStateRequested = GameTick.Zero;
        }

        public void RequestFullState()
        {
            _stateBuffer.Clear();
            LastFullState = null;
            LastFullStateRequested = _timing.LastRealTick;
        }

        public void MergeImplicitData(Dictionary<EntityUid, Dictionary<ushort, ComponentState>> data)
        {
            foreach (var (uid, compData) in data)
            {
                var fullRep = _lastStateFullRep[uid];

                foreach (var (netId, compState) in compData)
                {
                    if (!fullRep.ContainsKey(netId))
                    {
                        fullRep.Add(netId, compState);
                    }
                }
            }
        }

        public Dictionary<ushort, ComponentState> GetLastServerStates(EntityUid entity)
        {
            return _lastStateFullRep[entity];
        }

        public bool TryGetLastServerStates(EntityUid entity,
            [NotNullWhen(true)] out Dictionary<ushort, ComponentState>? dictionary)
        {
            return _lastStateFullRep.TryGetValue(entity, out dictionary);
        }

        public int CalculateBufferSize(GameTick fromTick)
        {
            bool foundState;
            var nextTick = fromTick;

            do
            {
                foundState = false;

                foreach (var state in _stateBuffer)
                {
                    if (state.ToSequence > nextTick && state.FromSequence <= nextTick)
                    {
                        foundState = true;
                        nextTick += 1;
                    }
                }

            }
            while (foundState);

            return (int) (nextTick.Value - fromTick.Value);
        }
    }
}
