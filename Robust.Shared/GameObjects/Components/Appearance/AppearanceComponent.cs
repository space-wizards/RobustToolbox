using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
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

        var stateDiff = _appearanceData.Count != actualState.Data.Count;

        if (!stateDiff)
        {
            foreach (var (key, value) in _appearanceData)
            {
                if (!actualState.Data.TryGetValue(key, out var stateValue) ||
                    !value.Equals(stateValue))
                {
                    stateDiff = true;
                    break;
                }
            }
        }

        if (!stateDiff) return;

        _appearanceData = CloneAppearanceData(actualState.Data);
        MarkDirty();
    }

    /// <summary>
    ///     Take in an appearance data dictionary and attempt to clone it.
    /// </summary>
    /// <remarks>
    ///     As some appearance data values are not simple value-type objects, this is not just a shallow clone.
    /// </remarks>
    private Dictionary<object, object> CloneAppearanceData(Dictionary<object, object> data)
    {
        Dictionary<object, object> newDict = new(data.Count);

        foreach (var (key, value) in data)
        {
            if (value.GetType().IsValueType)
                newDict[key] = value;
            else if (value is ICloneable cloneable)
                newDict[key] = cloneable.Clone();
            else
                throw new NotSupportedException("Invalid object in appearance data dictionary. Appearance data must be cloneable");
        }

        return newDict;
    }

    public void SetData(string key, object value)
    {
        if (_appearanceData.TryGetValue(key, out var existing) && existing.Equals(value))
            return;

        DebugTools.Assert(value.GetType().IsValueType || value is ICloneable, "Appearance data values must be cloneable.");

        _appearanceData[key] = value;
        Dirty();
        MarkDirty();
    }

    public void SetData(Enum key, object value)
    {
        if (_appearanceData.TryGetValue(key, out var existing) && existing.Equals(value))
            return;

        DebugTools.Assert(value.GetType().IsValueType || value is ICloneable, "Appearance data values must be cloneable.");

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
