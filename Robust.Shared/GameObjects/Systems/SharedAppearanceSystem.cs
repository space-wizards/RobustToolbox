using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract class SharedAppearanceSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AppearanceComponent, ComponentGetState>(OnAppearanceGetState);
    }

    protected abstract void OnAppearanceGetState(EntityUid uid, AppearanceComponent component,
        ref ComponentGetState args);

    /// <summary>
    ///     Mark an appearance component as dirty, so that the appearance will get updated in the next frame update.
    /// </summary>
    public virtual void QueueUpdate(EntityUid uid, AppearanceComponent component)
    {
    }

    private bool CheckIfApplyingState(AppearanceComponent component)
    {
        return _timing.ApplyingState && component.NetSyncEnabled; // TODO consider removing this and avoiding the component resolve altogether.
    }

    public void SetData(EntityUid uid, Enum key, object value, AppearanceComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        // If appearance data is changing due to server state application, the server's comp state is getting applied
        // anyways, so we can skip this.
        if (CheckIfApplyingState(component))
            return;

        if (component.AppearanceData.TryGetValue(key, out var existing) && existing.Equals(value))
            return;

        //Commented out until there is a suitable way to check that ISerializationManager.CopyTo works without doing the copying
        //DebugTools.Assert(value.GetType().IsValueType || value is ICloneable, "Appearance data values must be cloneable.");

        component.AppearanceData[key] = value;
        Dirty(uid, component);
        QueueUpdate(uid, component);
    }

    public void RemoveData(EntityUid uid, Enum key, AppearanceComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (CheckIfApplyingState(component))
            return;

        component.AppearanceData.Remove(key);
        
        Dirty(uid, component);
        QueueUpdate(uid, component);
    }

    public bool TryGetData<T>(EntityUid uid, Enum key, [NotNullWhen(true)] out T value, AppearanceComponent? component = null)
    {
        if (Resolve(uid, ref component) &&
            component.AppearanceData.TryGetValue(key, out var objValue) &&
            objValue is T)
        {
            value = (T)objValue;
            return true;
        }

        value = default!;
        return false;
    }

    public bool TryGetData(EntityUid uid, Enum key, [NotNullWhen(true)] out object? value, AppearanceComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            value = null;
            return false;
        }

        return component.AppearanceData.TryGetValue(key, out value);
    }

    /// <summary>
    /// Copies appearance data from <c>src</c> to <c>dest</c>.
    /// If <c>src</c> has no <see cref="AppearanceComponent"/> nothing is done.
    /// If <c>dest</c> has no <c>AppearanceComponent</c> then it is created.
    /// </summary>
    public void CopyData(Entity<AppearanceComponent?> src, Entity<AppearanceComponent?> dest)
    {
        if (!Resolve(src, ref src.Comp, false))
            return;

        dest.Comp ??= EnsureComp<AppearanceComponent>(dest);
        dest.Comp.AppearanceData.Clear();

        foreach (var (key, value) in src.Comp.AppearanceData)
        {
            dest.Comp.AppearanceData[key] = value;
        }

        Dirty(dest, dest.Comp);
        QueueUpdate(dest, dest.Comp);
    }

    /// <summary>
    /// Appends appearance data from <c>src</c> to <c>dest</c>. If a key/value pair already exists in <c>dest</c>, it gets replaced.
    /// If <c>src</c> has no <see cref="AppearanceComponent"/> nothing is done.
    /// If <c>dest</c> has no <c>AppearanceComponent</c> then it is created.
    /// </summary>
    public void AppendData(Entity<AppearanceComponent?> src, Entity<AppearanceComponent?> dest)
    {
        if (!Resolve(src, ref src.Comp, false))
            return;

        AppendData(src.Comp, dest);
    }

    public void AppendData(AppearanceComponent srcComp, Entity<AppearanceComponent?> dest)
    {
        dest.Comp ??= EnsureComp<AppearanceComponent>(dest);

        foreach (var (key, value) in srcComp.AppearanceData)
        {
            dest.Comp.AppearanceData[key] = value;
        }

        Dirty(dest, dest.Comp);
        QueueUpdate(dest, dest.Comp);
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
