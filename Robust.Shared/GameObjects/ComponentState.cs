using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [RequiresSerializable]
    [Serializable, NetSerializable]
    [Virtual]
    public class ComponentState : IComponentState
    {

    }

    /// <summary>
    /// Represents the state of a component for networking purposes.
    /// </summary>
    public interface IComponentState
    {

    }

    /// <summary>
    ///     Interface for components that support delta-states.
    /// </summary>
    public interface IComponentDeltaState
    {
        /// <summary>
        ///     Whether this state is a delta or full state.
        /// </summary>
        bool FullState { get; }

        /// <summary>
        ///     This function will apply the current delta state to the provided full state, modifying it in the process.
        /// </summary>
        public void ApplyToFullState(IComponentState fullState);

        /// <summary>
        ///     This function should take in a full state and return a new full state with the current delta applied,
        ///     WITHOUT modifying the original input state.
        /// </summary>
        public IComponentState CreateNewFullState(IComponentState fullState);
    }
}
