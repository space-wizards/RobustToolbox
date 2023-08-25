using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public abstract class SharedPointLightSystem : EntitySystem
{
    public void SetCastShadows(EntityUid uid, bool value, PointLightComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || value == comp.CastShadows)
            return;

        comp.CastShadows = value;
        Dirty(uid, comp);
    }

    public void SetColor(EntityUid uid, Color value, PointLightComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || value == comp.Color)
            return;

        comp.Color = value;
        Dirty(uid, comp);
    }

    public virtual void SetEnabled(EntityUid uid, bool enabled, PointLightComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || enabled == comp.Enabled)
            return;

        comp.Enabled = enabled;
        RaiseLocalEvent(uid, new PointLightToggleEvent(comp.Enabled));
        Dirty(uid, comp);
    }

    public virtual void SetRadius(EntityUid uid, float radius, PointLightComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || MathHelper.CloseToPercent(comp.Radius, radius))
            return;

        comp.Radius = radius;
        Dirty(uid, comp);
    }
}
