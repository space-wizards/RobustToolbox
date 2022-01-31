using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Client.GameObjects
{
    public sealed class PointLightSystem : SharedPointLightSystem
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly RenderingTreeSystem _renderingTreeSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PointLightComponent, ComponentInit>(HandleInit);
            SubscribeLocalEvent<PointLightComponent, ComponentRemove>(HandleRemove);
        }

        private void HandleInit(EntityUid uid, PointLightComponent component, ComponentInit args)
        {
            UpdateMask(component);
            RaiseLocalEvent(uid, new PointLightUpdateEvent());
        }

        private void HandleRemove(EntityUid uid, PointLightComponent component, ComponentRemove args)
        {
            if (Transform(uid).MapID != MapId.Nullspace)
            {
                _renderingTreeSystem.ClearLight(component);
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
