using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects;

/// <summary>
///     The appearance component allows game logic to be more detached from the actual visuals of an entity such as 2D
///     sprites, 3D, particles, lights...
///     It does this with a "data" system. Basically, code writes data to the component, and the component will use
///     prototype-based configuration to change the actual visuals.
///     The data works using a simple key/value system. It is recommended to use enum keys to prevent errors.
///     Visualization works client side with overrides of the <c>AppearanceVisualizer</c> class.
/// </summary>
[NetworkedComponent]
[ComponentProtoName("Appearance")]
public abstract class AppearanceComponent : Component
{
    [ViewVariables]
    private Dictionary<object, object> _appearanceData = new();

    public override ComponentState GetComponentState()
    {
        return new AppearanceComponentState(_appearanceData);
    }

    public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
    {
        if (curState is not AppearanceComponentState actualState)
            return;

        var stateDiff = false;

        // update server-side appearance data without discarding client-side data.
        foreach (var (key, newValue) in actualState.Data)
        {
            if (!_appearanceData.TryGetValue(key, out var currentValue) ||
                !currentValue.Equals(newValue))
            {
                stateDiff = true;
                _appearanceData[key] = newValue;
            }
        }

        if (stateDiff)
            MarkDirty();
    }

    public void SetData(string key, object value)
    {
        if (_appearanceData.TryGetValue(key, out var existing) && existing.Equals(value))
            return;

        _appearanceData[key] = value;
        Dirty();
        MarkDirty();
    }

    public void SetData(Enum key, object value)
    {
        if (_appearanceData.TryGetValue(key, out var existing) && existing.Equals(value))
            return;

        _appearanceData[key] = value;
        Dirty();
        MarkDirty();
    }

    public T GetData<T>(string key)
    {
        return (T)_appearanceData[key];
    }

    public T GetData<T>(Enum key)
    {
        return (T)_appearanceData[key];
    }

    public bool TryGetData<T>(Enum key, [NotNullWhen(true)] out T data)
    {
        return TryGetDataPrivate(key, out data);
    }

    public bool TryGetData<T>(string key, [NotNullWhen(true)] out T data)
    {
        return TryGetDataPrivate(key, out data);
    }

    protected virtual void MarkDirty() { }

    private bool TryGetDataPrivate<T>(object key, [NotNullWhen(true)] out T data)
    {
        if (_appearanceData.TryGetValue(key, out var dat))
        {
            data = (T)dat;
            return true;
        }

        data = default!;
        return false;
    }

    [Serializable, NetSerializable]
    protected class AppearanceComponentState : ComponentState
    {
        public readonly Dictionary<object, object> Data;

        public AppearanceComponentState(Dictionary<object, object> data)
        {
            Data = data;
        }
    }
}
