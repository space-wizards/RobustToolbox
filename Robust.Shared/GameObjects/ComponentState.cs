using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [RequiresSerializable]
    [Serializable, NetSerializable]
    [Virtual]
    public class ComponentState { }

    /// <summary>
    ///     Component states that implement this interface may or may not be delta-states.
    /// </summary>
    public interface IComponentDeltaState
    {
        /// <summary>
        ///     Whether this state is a delta or full state.
        /// </summary>
        bool FullState => false;
    }
}
