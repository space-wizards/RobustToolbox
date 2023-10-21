using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Robust.Server.GameStates;

/// <summary>
///     Placeholder system to expose some parts of the internal <see cref="PvsSystem"/> that allows entities to ignore
///     normal PVS rules, such that they are always sent to clients.
/// </summary>
public sealed class PvsOverrideSystem : EntitySystem
{
    [Shared.IoC.Dependency] private readonly PvsSystem _pvs = default!;

    /// <summary>
    ///     Used to ensure that an entity is always sent to every client. By default this overrides any client-specific overrides.
    /// </summary>
    /// <param name="removeExistingOverride">Whether or not to supersede existing overrides.</param>
    /// <param name="recursive">If true, this will also recursively send any children of the given index.</param>
    public void AddGlobalOverride(EntityUid uid, bool removeExistingOverride = true, bool recursive = false)
    {
        _pvs.EntityPVSCollection.AddGlobalOverride(GetNetEntity(uid), removeExistingOverride, recursive);
    }

    /// <summary>
    ///     Used to ensure that an entity is always sent to a specific client. By default this overrides any global or pre-existing
    ///     client-specific overrides. Unlike global overrides, this is always recursive.
    /// </summary>
    /// <param name="removeExistingOverride">Whether or not to supersede existing overrides.</param>
    /// <param name="recursive">If true, this will also recursively send any children of the given index.</param>
    public void AddSessionOverride(EntityUid uid, ICommonSession session, bool removeExistingOverride = true)
    {
        _pvs.EntityPVSCollection.AddSessionOverride(GetNetEntity(uid), session, removeExistingOverride);
    }

    /// <summary>
    ///     Removes any global or client-specific overrides.
    /// </summary>
    public void ClearOverride(EntityUid uid, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform))
            return;

        _pvs.EntityPVSCollection.UpdateIndex(GetNetEntity(uid), xform.Coordinates, true);
    }
}
