using System.Diagnostics.CodeAnalysis;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Client.GameObjects
{
    public sealed class PointLightSystem : SharedPointLightSystem
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PointLightComponent, ComponentGetState>(OnLightGetState);
            SubscribeLocalEvent<PointLightComponent, ComponentInit>(HandleInit);
            SubscribeLocalEvent<PointLightComponent, ComponentHandleState>(OnLightHandleState);
        }

        private void OnLightHandleState(EntityUid uid, PointLightComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not PointLightComponentState state)
                return;

            component.Enabled = state.Enabled;
            component.Offset = state.Offset;
            component.Softness = state.Softness;
            component.Falloff = state.Falloff;
            component.CurveFactor = state.CurveFactor;
            component.CastShadows = state.CastShadows;
            component.Energy = state.Energy;
            component.Radius = state.Radius;
            component.Color = state.Color;
            component.ContainerOccluded = state.ContainerOccluded;

            LightTree.QueueTreeUpdate(uid, component);
        }

        public override SharedPointLightComponent EnsureLight(EntityUid uid)
        {
            return EnsureComp<PointLightComponent>(uid);
        }

        public override bool ResolveLight(EntityUid uid, [NotNullWhen(true)] ref SharedPointLightComponent? component)
        {
            if (component is not null)
                return true;

            TryComp<PointLightComponent>(uid, out var comp);
            component = comp;
            return component != null;
        }

        public override bool TryGetLight(EntityUid uid, [NotNullWhen(true)] out SharedPointLightComponent? component)
        {
            if (TryComp<PointLightComponent>(uid, out var comp))
            {
                component = comp;
                return true;
            }

            component = null;
            return false;
        }

        public override bool RemoveLightDeferred(EntityUid uid)
        {
            return RemCompDeferred<PointLightComponent>(uid);
        }

        protected override void UpdatePriority(EntityUid uid, SharedPointLightComponent comp, MetaDataComponent meta)
        {
        }

        private void HandleInit(EntityUid uid, PointLightComponent component, ComponentInit args)
        {
            SetMask(component.LightMask, component);
        }

        public void SetMask(ProtoId<LightMaskPrototype>? lightMask, PointLightComponent component)
        {
            if (_proto.Resolve(lightMask, out var mask))
                component.Mask = _resourceCache.GetResource<TextureResource>(mask.MaskPath);

            else
                component.Mask = null;
        }
    }
}
