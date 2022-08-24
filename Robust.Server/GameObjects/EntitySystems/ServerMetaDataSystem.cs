using Robust.Server.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.GameObjects;

public sealed class ServerMetaDataSystem : MetaDataSystem
{
    [Dependency] private readonly PVSSystem _pvsSystem = default!;

    public override void SetVisibilityMask(EntityUid uid, int value, MetaDataComponent? meta = null)
    {
        if (!Resolve(uid, ref meta) || meta.VisibilityMask == value)
            return;

        base.SetVisibilityMask(uid, value, meta);
        _pvsSystem.MarkDirty(uid);
    }
}
