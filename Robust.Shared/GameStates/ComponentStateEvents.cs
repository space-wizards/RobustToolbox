using Robust.Shared.GameObjects;
using Robust.Shared.Players;

namespace Robust.Shared.GameStates
{
    public readonly struct ComponentHandleState
    {
        public ComponentState? Current { get; }
        public ComponentState? Next { get; }

        public ComponentHandleState(ComponentState? current, ComponentState? next)
        {
            Current = current;
            Next = next;
        }
    }

    /// <summary>
    ///     Component event for getting the component state for a specific player.
    /// </summary>
    public struct ComponentGetState
    {
        /// <summary>
        ///     Input parameter. The player the state is being generated for.
        /// </summary>
        public readonly ICommonSession Player;

        /// <summary>
        ///     Output parameter. Set this to the component's state for the player.
        /// </summary>
        public ComponentState? State { get; set; }

        public ComponentGetState(ICommonSession player)
        {
            Player = player;
            State = null;
        }
    }
}
