using System.Diagnostics.CodeAnalysis;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public abstract class SharedPointLightSystem : EntitySystem
{
    [Dependency] protected readonly SharedLightTreeSystem LightTree = default!;

    public abstract SharedPointLightComponent EnsureLight(EntityUid uid);

    public abstract bool ResolveLight(EntityUid uid, [NotNullWhen(true)] ref SharedPointLightComponent? component);

    public abstract bool TryGetLight(EntityUid uid, [NotNullWhen(true)] out SharedPointLightComponent? component);

    public abstract bool RemoveLightDeferred(EntityUid uid);

    protected abstract void UpdatePriority(EntityUid uid, SharedPointLightComponent comp, MetaDataComponent meta);

    public void UpdatePriority(Entity<SharedPointLightComponent?, MetaDataComponent?> ent)
    {
        if (Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2))
            UpdatePriority(ent.Owner, ent.Comp1, ent.Comp2);
    }

    public void SetCastShadows(EntityUid uid, bool value, SharedPointLightComponent? comp = null, MetaDataComponent? meta = null)
    {
        if (!ResolveLight(uid, ref comp) || value == comp.CastShadows)
            return;

        comp.CastShadows = value;
        if (!Resolve(uid, ref meta))
            return;

        Dirty(uid, comp, meta);
        UpdatePriority(uid, comp, meta);
    }

    public void SetColor(EntityUid uid, Color value, SharedPointLightComponent? comp = null)
    {
        if (!ResolveLight(uid, ref comp) || value == comp.Color)
            return;

        comp.Color = value;
        Dirty(uid, comp);
    }

    public void SetContainerOccluded(EntityUid uid, bool occluded, SharedPointLightComponent? comp = null)
    {
        if (!ResolveLight(uid, ref comp) || occluded == comp.ContainerOccluded)
            return;

        comp.ContainerOccluded = occluded;
        Dirty(uid, comp);
        if (comp.Enabled)
            LightTree.QueueTreeUpdate(uid, comp);
    }

    public void SetEnabled(EntityUid uid, bool enabled, SharedPointLightComponent? comp = null, MetaDataComponent? meta = null)
    {
        if (!ResolveLight(uid, ref comp) || enabled == comp.Enabled)
            return;

        var attempt = new AttemptPointLightToggleEvent(enabled);
        RaiseLocalEvent(uid, ref attempt);

        if (attempt.Cancelled)
            return;

        comp.Enabled = enabled;
        if (!comp.ContainerOccluded)
            LightTree.QueueTreeUpdate(uid, comp);

        RaiseLocalEvent(uid, new PointLightToggleEvent(comp.Enabled));
        if (!Resolve(uid, ref meta))
            return;

        Dirty(uid, comp, meta);
        UpdatePriority(uid, comp, meta);
    }

    public void SetEnergy(EntityUid uid, float value, SharedPointLightComponent? comp = null)
    {
        if (!ResolveLight(uid, ref comp) || MathHelper.CloseToPercent(comp.Energy, value))
            return;

        comp.Energy = value;
        Dirty(uid, comp);
    }

    public void SetRadius(EntityUid uid, float radius, SharedPointLightComponent? comp = null, MetaDataComponent? meta = null)
    {
        if (!ResolveLight(uid, ref comp) || MathHelper.CloseToPercent(comp.Radius, radius))
            return;

        comp.Radius = radius;
        if (comp.AddToTree)
            LightTree.QueueTreeUpdate(uid, comp);

        if (!Resolve(uid, ref meta))
            return;

        Dirty(uid, comp, meta);
        UpdatePriority(uid, comp, meta);
    }

    public void SetSoftness(EntityUid uid, float value, SharedPointLightComponent? comp = null)
    {
        if (!ResolveLight(uid, ref comp) || MathHelper.CloseToPercent(comp.Softness, value))
            return;

        comp.Softness = value;
        Dirty(uid, comp);
    }

    public void SetFalloff(EntityUid uid, float value, SharedPointLightComponent? comp = null)
    {
        if (!ResolveLight(uid, ref comp) || MathHelper.CloseToPercent(comp.Falloff, value))
            return;

        comp.Falloff = value;
        Dirty(uid, comp);
    }

    public void SetCurveFactor(EntityUid uid, float value, SharedPointLightComponent? comp = null)
    {
        if (!ResolveLight(uid, ref comp) || MathHelper.CloseToPercent(comp.CurveFactor, value))
            return;

        comp.CurveFactor = value;
        Dirty(uid, comp);
    }

    protected static void OnLightGetState(
        EntityUid uid,
        SharedPointLightComponent component,
        ref ComponentGetState args)
    {
        args.State = new PointLightComponentState()
        {
            Color = component.Color,
            Enabled = component.Enabled,
            Energy = component.Energy,
            Offset = component.Offset,
            Radius = component.Radius,
            Softness = component.Softness,
            Falloff = component.Falloff,
            CurveFactor = component.CurveFactor,
            CastShadows = component.CastShadows,
            ContainerOccluded = component.ContainerOccluded,
        };
    }
}
