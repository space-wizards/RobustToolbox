using Robust.Client.ComponentTrees;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects
{
    public sealed class PointLightSystem : SharedPointLightSystem
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly LightTreeSystem _lightTree = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PointLightComponent, ComponentInit>(HandleInit);
        }

        private void HandleInit(EntityUid uid, PointLightComponent component, ComponentInit args)
        {
            UpdateMask(component);
        }

        internal void UpdateMask(PointLightComponent component)
        {
            if (component._maskPath is not null)
                component.Mask = _resourceCache.GetResource<TextureResource>(component._maskPath);
            else
                component.Mask = null;
        }

        #region Setters
        public void SetContainerOccluded(EntityUid uid, bool occluded, PointLightComponent? comp = null)
        {
            if (!Resolve(uid, ref comp) || occluded == comp.ContainerOccluded)
                return;

            comp.ContainerOccluded = occluded;
            Dirty(comp);

            if (comp.Enabled)
                _lightTree.QueueTreeUpdate(uid, comp);
        }

        public override void SetEnabled(EntityUid uid, bool enabled, SharedPointLightComponent? comp = null)
        {
            if (!Resolve(uid, ref comp) || enabled == comp.Enabled)
                return;

            comp._enabled = enabled;
            RaiseLocalEvent(uid, new PointLightToggleEvent(comp.Enabled));
            Dirty(comp);

            var cast = (PointLightComponent)comp;
            if (!cast.ContainerOccluded)
                _lightTree.QueueTreeUpdate(uid, cast);
        }

        public override void SetRadius(EntityUid uid, float radius, SharedPointLightComponent? comp = null)
        {
            if (!Resolve(uid, ref comp) || MathHelper.CloseToPercent(radius, comp.Radius))
                return;

            comp._radius = radius;
            Dirty(comp);

            var cast = (PointLightComponent)comp;
            if (cast.TreeUid != null)
                _lightTree.QueueTreeUpdate(uid, cast);
        }
        #endregion
    }
}
