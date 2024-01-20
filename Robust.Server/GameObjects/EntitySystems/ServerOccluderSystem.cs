using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.GameObjects;

[UsedImplicitly]
public sealed class ServerOccluderSystem : OccluderSystem
{
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    protected override void OnCompStartup(EntityUid uid, OccluderComponent component, ComponentStartup args)
    {
        base.OnCompStartup(uid, component, args);
        _metadata.SetFlag(uid, MetaDataFlags.PvsPriority, component.Enabled);
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
