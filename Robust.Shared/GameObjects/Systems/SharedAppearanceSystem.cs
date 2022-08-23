using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract class SharedAppearanceSystem : EntitySystem
{
    public virtual void MarkDirty(AppearanceComponent component) {}

    public void SetData(EntityUid uid, Enum key, object value, AppearanceComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.AppearanceData.TryGetValue(key, out var existing) && existing.Equals(value))
            return;

        DebugTools.Assert(value.GetType().IsValueType || value is ICloneable, "Appearance data values must be cloneable.");

        component.AppearanceData[key] = value;
        Dirty(component);
        MarkDirty(component);
    }

    public bool TryGetData(EntityUid uid, Enum key, [MaybeNullWhen(false)] out object value, AppearanceComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            value = null;
            return false;
        }

        return component.AppearanceData.TryGetValue(key, out value);
    }
}

[Serializable, NetSerializable]
public sealed class AppearanceComponentState : ComponentState
{
    public readonly Dictionary<Enum, object> Data;

    public AppearanceComponentState(Dictionary<Enum, object> data)
    {
        Data = data;
    }
}
