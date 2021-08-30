using Robust.Shared.GameStates;

namespace Robust.Shared.GameObjects
{
    public abstract class SharedPointLightSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedPointLightComponent, ComponentHandleState>(HandleCompState);
        }

        private void HandleCompState(EntityUid uid, SharedPointLightComponent component, ComponentHandleState args)
        {
            if (args.Current is not PointLightComponentState newState) return;
            component.Enabled = newState.Enabled;
            component.Radius = newState.Radius;
            component.Offset = newState.Offset;
            component.Color = newState.Color;
        }
    }
}
