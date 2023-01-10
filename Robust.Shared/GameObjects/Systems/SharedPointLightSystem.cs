using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects
{
    public abstract class SharedPointLightSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedPointLightComponent, ComponentGetState>(GetCompState);
            SubscribeLocalEvent<SharedPointLightComponent, ComponentHandleState>(HandleCompState);
        }

        private void GetCompState(EntityUid uid, SharedPointLightComponent component, ref ComponentGetState args)
        {
            args.State = new PointLightComponentState(component.Enabled, component.Color, component.Radius, component.Offset, component.Energy, component.Softness, component.CastShadows);
        }

        private void HandleCompState(EntityUid uid, SharedPointLightComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not PointLightComponentState newState) return;

            SetEnabled(uid, newState.Enabled, component);
            SetRadius(uid, newState.Radius, component);
            component.Offset = newState.Offset;
            component.Color = newState.Color;
            component.Energy = newState.Energy;
            component.Softness = newState.Softness;
            component.CastShadows = newState.CastShadows;
        }

        public virtual void SetEnabled(EntityUid uid, bool enabled, SharedPointLightComponent? comp = null)
        {
            if (!Resolve(uid, ref comp) || enabled == comp.Enabled)
                return;

            comp._enabled = enabled;
            RaiseLocalEvent(uid, new PointLightToggleEvent(comp.Enabled));
            Dirty(comp);
        }

        public virtual void SetRadius(EntityUid uid, float radius, SharedPointLightComponent? comp = null)
        {
            if (!Resolve(uid, ref comp) || MathHelper.CloseToPercent(comp.Radius, radius))
                return;

            comp._radius = radius;
            Dirty(comp);
        }
    }
}
