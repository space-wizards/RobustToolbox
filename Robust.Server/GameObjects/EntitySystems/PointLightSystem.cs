using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Server.GameObjects
{
    internal sealed class PointLightSystem : EntitySystem
    {
        [Dependency] private readonly ExtendedPVSRangeSystem _pvsRange = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PointLightComponent, ComponentInit>(HandleInit);
            SubscribeLocalEvent<PointLightComponent, ComponentShutdown>(HandleShutdown);
        }

        private void HandleShutdown(EntityUid uid, PointLightComponent component, ComponentShutdown args)
        {
            _pvsRange.RemoveBounds(uid, component);
        }

        private void HandleInit(EntityUid uid, PointLightComponent component, ComponentInit args)
        {
            _pvsRange.AddBounds(uid, component, GetPvsRange(component));
        }

        internal Box2 GetPvsRange(PointLightComponent component)
        {
            if (!component.Enabled || component.ContainerOccluded)
            {
                return new Box2();
            }

            var offset = component.Offset;

            var aabb = new Box2(new Vector2(-component.Radius, -component.Radius) + offset,
                new Vector2(component.Radius, component.Radius) + offset);

            return aabb;
        }
    }
}
