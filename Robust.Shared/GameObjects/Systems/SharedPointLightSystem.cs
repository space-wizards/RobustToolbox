using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects
{
    public abstract class SharedPointLightSystem : EntitySystem
    {
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
}
