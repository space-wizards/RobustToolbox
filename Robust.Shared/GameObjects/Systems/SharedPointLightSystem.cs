using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public abstract class SharedPointLightSystem : EntitySystem
{
    public abstract SharedPointLightComponent EnsureLight(EntityUid uid);

    public abstract bool ResolveLight(EntityUid uid, [NotNullWhen(true)] ref SharedPointLightComponent? component);

    public abstract bool TryGetLight(EntityUid uid, [NotNullWhen(true)] out SharedPointLightComponent? component);

    public abstract bool RemoveLightDeferred(EntityUid uid);

    protected abstract void UpdatePriority(EntityUid uid, SharedPointLightComponent comp, MetaDataComponent meta);

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

    public virtual void SetEnabled(EntityUid uid, bool enabled, SharedPointLightComponent? comp = null, MetaDataComponent? meta = null)
    {
        if (!ResolveLight(uid, ref comp) || enabled == comp.Enabled)
            return;

        var attempt = new AttemptPointLightToggleEvent(enabled);
        RaiseLocalEvent(uid, ref attempt);

        if (attempt.Cancelled)
            return;

        comp.Enabled = enabled;
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

    public virtual void SetRadius(EntityUid uid, float radius, SharedPointLightComponent? comp = null, MetaDataComponent? meta = null)
    {
        if (!ResolveLight(uid, ref comp) || MathHelper.CloseToPercent(comp.Radius, radius))
            return;

        comp.Radius = radius;
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
}
