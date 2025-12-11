using System;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects;

/// <summary>
///     An abstract base class for a component's state. For simple cases, you can automatically generate this using
///     <see cref="AutoGenerateComponentStateAttribute"/> and <see cref="AutoNetworkedFieldAttribute"/>.<br/>
///     <br/>
///     If your component's state is particularly complex, or you otherwise want manual control, you can implement this
///     directly and register necessary event handlers for <see cref="ComponentHandleState"/> and
///     <see cref="ComponentGetState"/>.<br/>
///     <br/>
///     How state is actually applied for a component, and what it looks like, is user defined. For an example, look at
///     <see cref="OccluderComponent.OccluderComponentState"/>.
/// </summary>
[RequiresSerializable]
[Serializable, NetSerializable]
[Virtual]
public abstract class ComponentState : IComponentState;

/// <summary>
///     Represents the state of a component for networking purposes.
/// </summary>
public interface IComponentState;

/// <summary>
///     Internal for RT, you probably want <see cref="IComponentDeltaState{TState}"/>.
/// </summary>
public interface IComponentDeltaState : IComponentState
{
    public void ApplyToFullState(IComponentState fullState);

    public IComponentState CreateNewFullState(IComponentState fullState);
}

/// <summary>
///     Interface for component states that only contain partial state data. The actual delta state class should be a
///     separate class from the full component states.
/// </summary>
/// <typeparam name="TState">The full-state class associated with this partial state</typeparam>
public interface IComponentDeltaState<TState> : IComponentDeltaState where TState: IComponentState
{
    /// <summary>
    ///     This function will apply the current delta state to the provided full state, modifying it in the process.
    /// </summary>
    public void ApplyToFullState(TState fullState);

    /// <summary>
    ///     This function should take in a full state and return a new full state with the current delta applied,
    ///     WITHOUT modifying the original input state.
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
