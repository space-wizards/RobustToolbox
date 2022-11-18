using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [RequiresSerializable]
    [Serializable, NetSerializable]
    [Virtual]
    public class ComponentState { }

    public interface IComponentDeltaState
    {
        /// <summary>
        ///     Whether this state is a delta or full state.
        /// </summary>
        bool FullState => false;

        /// <summary>
        ///     This function takes a full and a delta state and apply the delta to the full state.
        /// </summary>
        public void ApplyToFullState(ComponentState fullState);

        /// <summary>
        ///     This function takes a full and a delta state and returns a new full state without modifying the original
        ///     full state.
        /// </summary>
        public ComponentState CreateNewFullState(ComponentState fullState);
    }
}
