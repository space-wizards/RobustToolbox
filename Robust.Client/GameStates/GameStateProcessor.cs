using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Robust.Client.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameStates
{
    /// <inheritdoc />
    internal sealed class GameStateProcessor : IGameStateProcessor
    {
        private readonly IClientGameTiming _timing;
        private readonly IClientGameStateManager _state;
        private readonly ISawmill _logger;

        private readonly List<GameState> _stateBuffer = new();

        private readonly Dictionary<GameTick, List<NetEntity>> _pvsDetachMessages = new();
        public GameState? LastFullState { get; private set; }
        public bool WaitingForFull => LastFullStateRequested.HasValue;
        public (GameTick Tick, DateTime Time)? LastFullStateRequested { get; private set; } = (GameTick.Zero, DateTime.MaxValue);

        private int _bufferSize;
        private int _maxBufferSize = 512;
        public const int MinimumMaxBufferSize = 256;

        /// <summary>
        /// This dictionary stores the full most recently received server state of any entity. This is used whenever predicted entities get reset.
        /// </summary>
        internal readonly Dictionary<NetEntity, Dictionary<ushort, IComponentState?>> _lastStateFullRep
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
            set => _bufferSize = Math.Max(value, 0);
        }

        public int MaxBufferSize
        {
            get => _maxBufferSize;
            // We place a lower bound on the maximum size to avoid spamming servers with full game state requests.
            set => _maxBufferSize = Math.Max(value, MinimumMaxBufferSize);
        }

        /// <inheritdoc />
        public bool Logging { get; set; }

        /// <summary>
        ///     Constructs a new instance of <see cref="GameStateProcessor"/>.
        /// </summary>
        /// <param name="timing">Timing information of the current state.</param>
        /// <param name="clientGameStateManager"></param>
        public GameStateProcessor(IClientGameStateManager state, IClientGameTiming timing, ISawmill logger)
        {
            _timing = timing;
            _state = state;
            _logger = logger;
        }

        /// <inheritdoc />
        public bool AddNewState(GameState state)
        {
            // Check for old states.
            if (state.ToSequence <= _timing.LastRealTick)
            {
                if (Logging)
                    _logger.Debug($"Received Old GameState: lastRealTick={_timing.LastRealTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={state.PayloadSize}, buf={_stateBuffer.Count}");

                return false;
            }

            // Check for a duplicate states.
            foreach (var bufferState in _stateBuffer)
            {
                if (state.ToSequence != bufferState.ToSequence)
                    continue;

                if (Logging)
                    _logger.Debug($"Received Dupe GameState: lastRealTick={_timing.LastRealTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={state.PayloadSize}, buf={_stateBuffer.Count}");

                return false;
            }

            // Are we expecting a full state?
            if (!WaitingForFull)
            {
                // This is a good state that we will be using.
                TryAdd(state);
                if (Logging)
                    _logger.Debug($"Received New GameState: lastRealTick={_timing.LastRealTick}, fSeq={state.FromSequence}, tSeq={state.ToSequence}, sz={state.PayloadSize}, buf={_stateBuffer.Count}");
                return true;
            }

            if (LastFullState == null && state.FromSequence == GameTick.Zero)
            {
                if (state.ToSequence >= LastFullStateRequested!.Value.Tick)
                {
                    LastFullState = state;
                    _logger.Info($"Received Full GameState: to={state.ToSequence}, sz={state.PayloadSize}");
                    return true;
                }

                _logger.Info($"Received a late full game state. Received: {state.ToSequence}. Requested: {LastFullStateRequested.Value.Tick}");
            }

            if (LastFullState != null && state.ToSequence <= LastFullState.ToSequence)
            {
                _logger.Info($"While waiting for full, received late GameState with lower to={state.ToSequence} than the last full state={LastFullState.ToSequence}");
                return false;
            }

            TryAdd(state);
            return true;
        }

        public void TryAdd(GameState state)
        {
            if (_stateBuffer.Count <= MaxBufferSize)
            {
                _stateBuffer.Add(state);
                return;
            }

            // This can happen if a required state gets dropped somehow and the client keeps receiving future
            // game states that they can't apply. I.e., GetApplicableStateCount() is zero, even though there are many
            // states in the list.
            //
            // This can seemingly happen when the server sends ""reliable"" game states while the client is paused?
            // For example, when debugging the client, while the server is running:
            // - The client stops sending acks for states that the server sends out.
            // - Thus the client will exceed the net.force_ack_threshold cvar
            // - The server starts sending some packets ""reliably"" and just force updates the clients last ack.
            //
            // What should happen is that when the client resumes, it receives the reliably sent states and can just
            // resume. However, even though the packets are sent ""reliably"", they just seem to get dropped.
            // I don't quite understand how/why yet, but this ensures the client doesn't get stuck.
#if FULL_RELEASE
            _logger.Warning(@$"Exceeded maximum state buffer size!
Tick: {_timing.CurTick}/{_timing.LastProcessedTick}/{_timing.LastRealTick}
Size: {_stateBuffer.Count}
Applicable states: {GetApplicableStateCount()}
Was waiting for full: {WaitingForFull} {LastFullStateRequested}
Had full state: {LastFullState != null}"
            );
#endif

            _state.RequestFullState();
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
                    _logger.Debug($"Applying State:  cTick={_timing.LastProcessedTick}, fSeq={curState.FromSequence}, tSeq={curState.ToSequence}, buf={_stateBuffer.Count}");
            }

            return applyNextState;
        }

        public void UpdateFullRep(GameState state, bool cloneDelta = false)
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
                if (!_lastStateFullRep.TryGetValue(entityState.NetEntity, out var compData))
                {
                    compData = new();
                    _lastStateFullRep.Add(entityState.NetEntity, compData);
                }

                foreach (var change in entityState.ComponentChanges.Span)
                {
                    var compState = change.State;

                    if (compState is IComponentDeltaState delta
                        && compData.TryGetValue(change.NetID, out var old)) // May fail if relying on implicit data
                    {
                        DebugTools.Assert(old is not IComponentDeltaState, "last state is not a full state");

                        if (cloneDelta)
                        {
                            compState = delta.CreateNewFullState(old!);
                        }
                        else
                        {
                            delta.ApplyToFullState(old!);
                            compState = old;
                        }
                        DebugTools.Assert(compState is not IComponentDeltaState, "newly constructed state is not a full state");
                    }

                    compData[change.NetID] = compState;
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
                    _logger.Debug($"Resync CurTick to: {LastFullState.ToSequence}");

                curState = LastFullState;
                return true;
            }

            // waiting for buffer to fill
            if (Logging)
                _logger.Debug($"Have FullState, filling buffer... ({_stateBuffer.Count}/{TargetBufferSize})");

            return false;
        }

        internal void AddLeavePvsMessage(List<NetEntity> entities, GameTick tick)
        {
            // Late message may still need to be processed,
            DebugTools.Assert(entities.Count > 0);
            _pvsDetachMessages.TryAdd(tick, entities);
        }

        public void ClearDetachQueue() => _pvsDetachMessages.Clear();

        public List<(GameTick Tick, List<NetEntity> Entities)> GetEntitiesToDetach(GameTick toTick, int budget)
        {
            var result = new List<(GameTick Tick, List<NetEntity> Entities)>();
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
            LastFullStateRequested = (GameTick.Zero, DateTime.MaxValue);
        }

        public void OnFullStateRequested(GameTick tick)
        {
            _stateBuffer.Clear();
            LastFullState = null;
            LastFullStateRequested = (tick, DateTime.UtcNow);
        }

        public void OnFullStateReceived()
        {
            LastFullState = null;
            LastFullStateRequested = null;
        }

        public void MergeImplicitData(Dictionary<NetEntity, Dictionary<ushort, IComponentState?>> implicitData)
        {
            foreach (var (netEntity, implicitEntState) in implicitData)
            {
                var fullRep = _lastStateFullRep[netEntity];

                foreach (var (netId, implicitCompState) in implicitEntState)
                {
                    DebugTools.Assert(implicitCompState is not IComponentDeltaState);
                    ref var serverState = ref CollectionsMarshal.GetValueRefOrAddDefault(fullRep, netId, out var exists);

                    if (!exists)
                    {
                        serverState = implicitCompState;
                        continue;
                    }

                    if (serverState is not IComponentDeltaState serverDelta)
                        continue;

                    DebugTools.AssertNotNull(implicitCompState);

                    // Server sent an initial delta state. This is fine as long as the client can infer an initial full
                    // state from the entity prototype.
                    serverDelta.ApplyToFullState(implicitCompState!);
                    serverState = implicitCompState;
                    DebugTools.Assert(serverState is not IComponentDeltaState);
                }
            }
        }

        public Dictionary<ushort, IComponentState?> GetLastServerStates(NetEntity netEntity)
        {
            return _lastStateFullRep[netEntity];
        }

        public Dictionary<NetEntity, Dictionary<ushort, IComponentState?>> GetFullRep()
        {
            return _lastStateFullRep;
        }

        public bool TryGetLastServerStates(NetEntity entity,
            [NotNullWhen(true)] out Dictionary<ushort, IComponentState?>? dictionary)
        {
            return _lastStateFullRep.TryGetValue(entity, out dictionary);
        }

        public bool IsQueuedForDetach(NetEntity entity)
        {
            // This isn't fast, but its just meant for use in tests & debug asserts.
            foreach (var msg in _pvsDetachMessages.Values)
            {
                if (msg.Contains(entity))
                    return true;
            }

            return false;
        }

        public int GetApplicableStateCount(GameTick? fromTick = null)
        {
            fromTick ??= _timing.LastRealTick;
            bool foundState;
            var nextTick = fromTick.Value;

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

            return (int) (nextTick.Value - fromTick.Value.Value);
        }

        public int StateCount => _stateBuffer.Count;
    }
}
