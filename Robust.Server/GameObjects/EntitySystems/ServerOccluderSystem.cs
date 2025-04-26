using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.GameObjects;

[UsedImplicitly]
public sealed class ServerOccluderSystem : OccluderSystem
{
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OccluderComponent, MetaFlagRemoveAttemptEvent>(OnFlagRemoveAttempt);
    }

    private void OnFlagRemoveAttempt(Entity<OccluderComponent> ent, ref MetaFlagRemoveAttemptEvent args)
    {
        if (ent.Comp is {Enabled: true, LifeStage: <= ComponentLifeStage.Running})
            args.ToRemove &= ~MetaDataFlags.PvsPriority;
    }

    protected override void OnCompStartup(EntityUid uid, OccluderComponent component, ComponentStartup args)
    {
        base.OnCompStartup(uid, component, args);
        _metadata.SetFlag(uid, MetaDataFlags.PvsPriority, component.Enabled);
    }

    protected override void OnCompRemoved(EntityUid uid, OccluderComponent component, ComponentRemove args)
    {
        base.OnCompRemoved(uid, component, args);
        _metadata.SetFlag(uid, MetaDataFlags.PvsPriority, false);
    }

    public override void SetEnabled(EntityUid uid, bool enabled, OccluderComponent? comp = null, MetaDataComponent? meta = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        if (enabled == comp.Enabled)
            return;

        if (!Resolve(uid, ref meta))
            return;

        base.SetEnabled(uid, enabled, comp, meta);
        _metadata.SetFlag((uid, meta), MetaDataFlags.PvsPriority, enabled);
    }
}
