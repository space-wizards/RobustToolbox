using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Client.GameObjects
{
    public sealed class PointLightSystem : SharedPointLightSystem
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PointLightComponent, ComponentInit>(HandleInit);
            SubscribeLocalEvent<PointLightComponent, ComponentRemove>(HandleRemove);
        }

        private void HandleInit(EntityUid uid, PointLightComponent component, ComponentInit args)
        {
            UpdateMask(component);
        }

        private void HandleRemove(EntityUid uid, PointLightComponent component, ComponentRemove args)
        {
            var map = EntityManager.GetComponent<TransformComponent>(uid).MapID;
            // TODO: Just make this update the tree directly and not allocate
            if (map != MapId.Nullspace)
            {
                EntityManager.EventBus.RaiseEvent(EventSource.Local,
                    new RenderTreeRemoveLightEvent(component, map));
            }
        }

        internal void UpdateMask(PointLightComponent component)
        {
            if (component._maskPath is not null)
                component.Mask = _resourceCache.GetResource<TextureResource>(component._maskPath);
            else
                component.Mask = null;
        }
    }
}
