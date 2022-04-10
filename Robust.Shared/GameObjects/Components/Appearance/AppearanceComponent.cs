using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;
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
    [ViewVariables] internal bool AppearanceDirty;

    [ViewVariables] internal Dictionary<object, object> AppearanceData = new();

    public void SetData(string key, object value)
    {
        if (AppearanceData.TryGetValue(key, out var existing) && existing.Equals(value))
            return;

        DebugTools.Assert(value.GetType().IsValueType || value is ICloneable, "Appearance data values must be cloneable.");

        AppearanceData[key] = value;
        Dirty();
        EntitySystem.Get<SharedAppearanceSystem>().MarkDirty(this);
    }

    public void SetData(Enum key, object value)
    {
        if (AppearanceData.TryGetValue(key, out var existing) && existing.Equals(value))
            return;

        DebugTools.Assert(value.GetType().IsValueType || value is ICloneable, "Appearance data values must be cloneable.");

        AppearanceData[key] = value;
        Dirty();
        EntitySystem.Get<SharedAppearanceSystem>().MarkDirty(this);
    }

    public T GetData<T>(string key)
    {
        return (T)AppearanceData[key];
    }

    public T GetData<T>(Enum key)
    {
        return (T)AppearanceData[key];
    }

    public bool TryGetData<T>(Enum key, [NotNullWhen(true)] out T data)
    {
        return TryGetDataPrivate(key, out data);
    }

    public bool TryGetData<T>(string key, [NotNullWhen(true)] out T data)
    {
        return TryGetDataPrivate(key, out data);
    }

    private bool TryGetDataPrivate<T>(object key, [NotNullWhen(true)] out T data)
    {
        if (AppearanceData.TryGetValue(key, out var dat))
        {
            data = (T)dat;
            return true;
        }

        data = default!;
        return false;
    }
}
