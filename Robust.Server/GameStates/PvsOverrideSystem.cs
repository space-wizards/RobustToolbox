using Robust.Shared.GameObjects;
using Robust.Shared.Players;

namespace Robust.Server.GameStates;

/// <summary>
///     Placeholder system to expose some parts of the internal <see cref="PvsSystem"/> that allows entities to ignore
///     normal PVS rules, such that they are always sent to clients.
/// </summary>
public sealed class PvsOverrideSystem : EntitySystem
{
    [Shared.IoC.Dependency] private readonly PvsSystem _pvs = default!;

    /// <summary>
    ///     Used to ensure that an entity is always sent to every client. Overrides any client-specific overrides.
    /// </summary>
    public void AddGlobalOverride(EntityUid uid)
    {
        _pvs.EntityPVSCollection.UpdateIndex(uid, true);
    }

    /// <summary>
    ///     Used to ensure that an entity is always sent to a specific client. Overrides any global or pre-existing
    ///     client-specific overrides.
    /// </summary>
    public void AddSessionOverride(EntityUid uid, ICommonSession session)
    {
        _pvs.EntityPVSCollection.UpdateIndex(uid, session, true);
    }

    /// <summary>
    ///     Removes any global or client-specific overrides.
    /// </summary>
    public void ClearOverride(EntityUid uid, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform))
            return;

        _pvs.EntityPVSCollection.UpdateIndex(uid, xform.Coordinates, true);
    }
}
