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
    [Dependency] private readonly IReflectionManager _refMan = default!;

    public virtual void MarkDirty(AppearanceComponent component) {}

    #region SetData
    public void SetData(EntityUid uid, string key, object value, AppearanceComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (_refMan.TryParseEnumReference(key, out var @enum))
            SetDataPrivate(component, @enum, value);
        else
            SetDataPrivate(component, key, value);
    }

    public void SetData(EntityUid uid, Enum key, object value, AppearanceComponent? component = null)
    {
        if (Resolve(uid, ref component))
            SetDataPrivate(component, key, value);
    }

    private void SetDataPrivate(AppearanceComponent component, object key, object value)
    {
        if (component.AppearanceData.TryGetValue(key, out var existing) && existing.Equals(value))
            return;

        DebugTools.Assert(value.GetType().IsValueType || value is ICloneable, "Appearance data values must be cloneable.");

        component.AppearanceData[key] = value;
        Dirty(component);
        MarkDirty(component);
    }
    #endregion

    #region TryGetData
    public bool TryGetData(EntityUid uid, string key, [MaybeNullWhen(false)] out object value, AppearanceComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            value = null;
            return false;
        }

        if (_refMan.TryParseEnumReference(key, out var @enum))
            return component.AppearanceData.TryGetValue(@enum, out value);
        else
            return component.AppearanceData.TryGetValue(key, out value);
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
    #endregion
}

[Serializable, NetSerializable]
public sealed class AppearanceComponentState : ComponentState
{
    public readonly Dictionary<object, object> Data;

    public AppearanceComponentState(Dictionary<object, object> data)
    {
        Data = data;
    }
}
