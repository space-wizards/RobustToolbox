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

public interface IComponentDeltaState : IComponentState
{
    public void ApplyToFullState(IComponentState fullState);

    public IComponentState CreateNewFullState(IComponentState fullState);
}

/// <summary>
/// Interface for component states that only contain partial state data. The actual delta state class should be a
/// separate class from the full component states.
/// </summary>
/// <typeparam name="TState">The full-state class associated with this partial state</typeparam>
public interface IComponentDeltaState<TState> : IComponentDeltaState where TState: IComponentState
{
    /// <summary>
    /// This function will apply the current delta state to the provided full state, modifying it in the process.
    /// </summary>
    public void ApplyToFullState(TState fullState);

    /// <summary>
    /// This function should take in a full state and return a new full state with the current delta applied, WITHOUT
    /// modifying the original input state.
    /// </summary>
    public TState CreateNewFullState(TState fullState);

    void IComponentDeltaState.ApplyToFullState(IComponentState fullState)
    {
        if (fullState is not TState state)
            throw new Exception($"Unexpected type. Expected {typeof(TState).Name} but got {fullState.GetType().Name}");

        ApplyToFullState(state);
    }

    IComponentState IComponentDeltaState.CreateNewFullState(IComponentState fullState)
    {
        if (fullState is not TState state)
            throw new Exception($"Unexpected type. Expected {typeof(TState).Name} but got {fullState.GetType().Name}");

        return CreateNewFullState(state);
    }
}
