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
            SetMask(component.MaskPath, component);
        }

        public void SetMask(string? maskPath, PointLightComponent component)
        {
            if (maskPath is not null)
                component.Mask = _resourceCache.GetResource<TextureResource>(maskPath);
            else
                component.Mask = null;
        }

        #region Setters
        public void SetContainerOccluded(EntityUid uid, bool occluded, PointLightComponent? comp = null)
        {
            if (!Resolve(uid, ref comp) || occluded == comp.ContainerOccluded)
                return;

            comp.ContainerOccluded = occluded;
            Dirty(uid, comp);

            if (comp.Enabled)
                _lightTree.QueueTreeUpdate(uid, comp);
        }

        public override void SetEnabled(EntityUid uid, bool enabled, PointLightComponent? comp = null)
        {
            if (!Resolve(uid, ref comp) || enabled == comp.Enabled)
                return;

            comp.Enabled = enabled;
            RaiseLocalEvent(uid, new PointLightToggleEvent(comp.Enabled));
            Dirty(uid, comp);

            var cast = (PointLightComponent)comp;
            if (!cast.ContainerOccluded)
                _lightTree.QueueTreeUpdate(uid, cast);
        }

        public override void SetRadius(EntityUid uid, float radius, PointLightComponent? comp = null)
        {
            if (!Resolve(uid, ref comp) || MathHelper.CloseToPercent(radius, comp.Radius))
                return;

            comp.Radius = radius;
            Dirty(uid, comp);

            var cast = (PointLightComponent)comp;
            if (cast.TreeUid != null)
                _lightTree.QueueTreeUpdate(uid, cast);
        }
        #endregion
    }
}
