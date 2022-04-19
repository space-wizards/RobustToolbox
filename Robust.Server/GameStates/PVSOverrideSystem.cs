using Robust.Shared.GameObjects;

namespace Robust.Server.GameStates;

/// <summary>
///     Placeholder system to expose some parts of the internal <see cref="PVSSystem"/> that allows entities to ignore
///     normal PVS rules, such that they are always sent to clients.
/// </summary>
public sealed partial class PVSOverrideSystem : EntitySystem
{
    [Shared.IoC.Dependency] private readonly PVSSystem _pvs = default!;

    public void AddGlobalOverride(EntityUid uid)
    {
        _pvs.EntityPVSCollection.UpdateIndex(uid, true);
    }
}
