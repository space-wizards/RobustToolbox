using Robust.Shared.GameObjects;
using Robust.Shared.Players;

namespace Robust.Shared.GameStates
{
    public struct ComponentHandleState
    {
        public ComponentState? Current { get; }
        public ComponentState? Next { get; }

        public ComponentHandleState(ComponentState? current, ComponentState? next)
        {
            Current = current;
            Next = next;
        }
    }
}
