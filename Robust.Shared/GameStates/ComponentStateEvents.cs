using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameStates
{
    [ByRefEvent, ComponentEvent]
    public readonly struct ComponentHandleState
    {
        public IComponentState? Current { get; }
        public IComponentState? Next { get; }

        public ComponentHandleState(IComponentState? current, IComponentState? next)
        {
            DebugTools.Assert(current != null || next != null);
            Current = current;
            Next = next;
        }
    }

    /// <summary>
    ///     Component event for getting the component state for a specific player.
    /// </summary>
    [ByRefEvent, ComponentEvent]
    public struct ComponentGetState
    {
        public GameTick FromTick { get; }

        /// <summary>
        ///     Output parameter. Set this to the component's state for the player.
        /// </summary>
        public IComponentState? State { get; set; }

        /// <summary>
        ///     If true, this state is intended for replays or some other server spectator entity, not for specific
        ///     clients.
        /// </summary>
        [MemberNotNullWhen(false, nameof(Player))]
        public bool ReplayState => Player == null;

        /// <summary>
        ///     The player the state is being sent to. Null implies the state is for a replay or some spectator entity.
        /// </summary>
        public readonly ICommonSession? Player;

        public ComponentGetState(ICommonSession? player, GameTick fromTick)
        {
            Player = player;
            FromTick = fromTick;
            State = null;
        }
    }

    [ByRefEvent, ComponentEvent]
    public struct ComponentGetStateAttemptEvent
    {
        /// <summary>
        ///     Input parameter. The player the state is being sent to. This may be null if the state is for replay recordings.
        /// </summary>
        public readonly ICommonSession? Player;

        public bool Cancelled = false;

        public ComponentGetStateAttemptEvent(ICommonSession? player)
        {
            Player = player;
        }
    }
}
