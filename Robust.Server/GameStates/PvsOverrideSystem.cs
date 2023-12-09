using System.Collections.Generic;
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
    /// Yields the NetEntity session overrides for the specified session.
    /// </summary>
    /// <param name="session"></param>
    public IEnumerable<NetEntity> GetSessionOverrides(ICommonSession session)
    {
        var enumerator = _pvs.EntityPVSCollection.GetSessionOverrides(session);

        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            yield return current;
        }
    }

    /// <summary>
    ///     Used to ensure that an entity is always sent to every client. By default this overrides any client-specific overrides.
    /// </summary>
    /// <param name="removeExistingOverride">Whether or not to supersede existing overrides.</param>
    /// <param name="recursive">If true, this will also recursively send any children of the given index.</param>
    public void AddGlobalOverride(NetEntity entity, bool removeExistingOverride = true, bool recursive = false)
    {
        _pvs.EntityPVSCollection.AddGlobalOverride(entity, removeExistingOverride, recursive);
    }

    /// <summary>
    ///     Used to ensure that an entity is always sent to a specific client. Overrides any global or pre-existing
    ///     client-specific overrides.
    /// </summary>
    /// <param name="removeExistingOverride">Whether or not to supersede existing overrides.</param>
    public void AddSessionOverride(NetEntity entity, ICommonSession session, bool removeExistingOverride = true)
    {
        _pvs.EntityPVSCollection.AddSessionOverride(entity, session, removeExistingOverride);
    }

    // 'placeholder'
    public void AddSessionOverrides(NetEntity entity, Filter filter, bool removeExistingOverride = true)
    {
        foreach (var player in filter.Recipients)
        {
            AddSessionOverride(entity, player, removeExistingOverride);
        }
    }

    /// <summary>
    ///     Removes any global or client-specific overrides.
    /// </summary>
    public void ClearOverride(NetEntity entity, TransformComponent? xform = null)
    {
        if (!TryGetEntity(entity, out var uid) || !Resolve(uid.Value, ref xform))
            return;

        _pvs.EntityPVSCollection.UpdateIndex(entity, xform.Coordinates, true);
    }
}
