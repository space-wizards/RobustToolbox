using Robust.Shared.GameObjects;

namespace Robust.Shared.GameStates
{
    public class ComponentHandleState : EntityEventArgs
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
