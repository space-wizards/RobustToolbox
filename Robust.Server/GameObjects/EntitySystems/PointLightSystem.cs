using System.Diagnostics.CodeAnalysis;

using Robust.Server.ComponentTrees;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Server.GameObjects;

    public sealed class PointLightSystem : SharedPointLightSystem
{
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly LightTreeSystem _lightTree = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PointLightComponent, ComponentGetState>(OnLightGetState);
        SubscribeLocalEvent<PointLightComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<PointLightComponent, EntGotRemovedFromContainerMessage>(OnRemoved);
    }

    /// This is public just so that the LightTreeSystem can call it
    public void OnLightStartup(EntityUid uid, PointLightComponent component, ComponentStartup args)
    {
        UpdatePriority(uid, component, MetaData(uid));
    }

    protected override void UpdatePriority(EntityUid uid, SharedPointLightComponent comp, MetaDataComponent meta)
    {
        var isHighPriority = comp.Enabled && comp.CastShadows && (comp.Radius > 7);
        _metadata.SetFlag((uid, meta), MetaDataFlags.PvsPriority, isHighPriority);
    }

    private void OnInserted(EntityUid uid, PointLightComponent component, EntGotInsertedIntoContainerMessage args)
    {
        SetContainerOccluded(uid, args.Container.OccludesLight, component);
    }

    private void OnRemoved(EntityUid uid, PointLightComponent component, EntGotRemovedFromContainerMessage args)
    {
        SetContainerOccluded(uid, false, component);
    }

    private void OnLightGetState(EntityUid uid, PointLightComponent component, ref ComponentGetState args)
    {
        args.State = new PointLightComponentState()
        {
            Color = component.Color,
            Enabled = component.Enabled,
            Energy = component.Energy,
            Offset = component.Offset,
            Radius = component.Radius,
            Softness = component.Softness,
            CastShadows = component.CastShadows,
            ContainerOccluded = component.ContainerOccluded,
        };

        _lightTree.QueueTreeUpdate(uid, component);
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

    public override void SetContainerOccluded(EntityUid uid, bool occluded, SharedPointLightComponent? comp = null)
    {
        if (!ResolveLight(uid, ref comp) || occluded == comp.ContainerOccluded || comp is not PointLightComponent clientComp)
            return;

        base.SetContainerOccluded(uid, occluded, comp);
        if (comp.Enabled)
            _lightTree.QueueTreeUpdate(uid, clientComp);
    }

    public override void SetEnabled(EntityUid uid, bool enabled, SharedPointLightComponent? comp = null, MetaDataComponent? meta = null)
    {
        if (!ResolveLight(uid, ref comp) || enabled == comp.Enabled || comp is not PointLightComponent clientComp)
            return;

        base.SetEnabled(uid, enabled, comp, meta);
        if (!comp.ContainerOccluded)
            _lightTree.QueueTreeUpdate(uid, clientComp);
    }

    public override void SetRadius(EntityUid uid, float radius, SharedPointLightComponent? comp = null, MetaDataComponent? meta = null)
    {
        if (!ResolveLight(uid, ref comp) || MathHelper.CloseToPercent(radius, comp.Radius) ||
            comp is not PointLightComponent clientComp)
            return;

        base.SetRadius(uid, radius, comp, meta);
        if (clientComp.TreeUid != null)
            _lightTree.QueueTreeUpdate(uid, clientComp);
    }
}
