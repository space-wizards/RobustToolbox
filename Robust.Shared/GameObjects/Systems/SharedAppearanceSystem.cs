using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract class SharedAppearanceSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public virtual void MarkDirty(AppearanceComponent component) {}

    public void SetData(EntityUid uid, Enum key, object value, AppearanceComponent? component = null)
    {
        // If appearance data is changing due to server state application, the server's comp state is getting applied
        // anyways, so we can skip this.
        if (_timing.ApplyingState)
            return; 

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
