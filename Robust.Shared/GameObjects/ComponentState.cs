using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects;

[RequiresSerializable]
[Serializable, NetSerializable]
[Virtual]
public abstract class ComponentState : IComponentState;

/// <summary>
/// Represents the state of a component for networking purposes.
/// </summary>
public interface IComponentState;

/// <summary>
///     Interface for component states that only contain partial state data.
///     The actual delta state class should be a separate class from the full component states.
/// </summary>
public interface IComponentDeltaState
{
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

public interface IComponentDeltaState<TState> : IComponentState where TState: IComponentState
{
    public void ApplyToFullState(TState fullState);

    public TState CreateNewFullState(TState fullState);
}
