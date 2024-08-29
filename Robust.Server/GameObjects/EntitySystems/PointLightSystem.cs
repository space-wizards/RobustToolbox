using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;

namespace Robust.Server.GameObjects;

public sealed class PointLightSystem : SharedPointLightSystem
{
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PointLightComponent, ComponentGetState>(OnLightGetState);
        SubscribeLocalEvent<PointLightComponent, ComponentStartup>(OnLightStartup);
    }

    private void OnLightStartup(EntityUid uid, PointLightComponent component, ComponentStartup args)
    {
        UpdatePriority(uid, component, MetaData(uid));
    }

    protected override void UpdatePriority(EntityUid uid, SharedPointLightComponent comp, MetaDataComponent meta)
    {
        var isHighPriority = comp.Enabled && comp.CastShadows && (comp.Radius > 7);
        _metadata.SetFlag((uid, meta), MetaDataFlags.PvsPriority, isHighPriority);
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
        };
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
}
