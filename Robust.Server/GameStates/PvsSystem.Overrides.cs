using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Robust.Server.GameStates;

// This partial class contains code for handling sending PVS overrides, i.e., entities that ignore normal PVS
// range/chunk restrictions.
internal sealed partial class PvsSystem
{
    private readonly List<PvsChunk.ChunkEntity> _cachedForceOverride = new();
    private readonly List<PvsChunk.ChunkEntity> _cachedGlobalOverride = new();

    private readonly HashSet<EntityUid> _forceOverrideSet = new();
    private readonly HashSet<EntityUid> _globalOverrideSet = new();

    private void AddAllOverrides(PvsSession session)
    {
        var mask = session.VisMask;
        var fromTick = session.FromTick;
        RaiseExpandEvent(session, fromTick);

        foreach (ref var ent in CollectionsMarshal.AsSpan(_cachedGlobalOverride))
        {
            ref var meta = ref _metadataMemory.GetRef(ent.Ptr.Index);
            if ((mask & meta.VisMask) == meta.VisMask)
                AddEntity(session, ref ent, ref meta, fromTick);
        }

        if (!_pvsOverride.SessionOverrides.TryGetValue(session.Session, out var sessionOverrides))
            return;

        foreach (var uid in sessionOverrides)
        {
            RecursivelyAddOverride(session, uid, fromTick, addChildren: true);
        }
    }

    /// <summary>
    /// Adds all entities that ignore normal pvs budgets.
    /// </summary>
    private void AddForcedEntities(PvsSession session)
    {
        // Ignore PVS budgets
        session.Budget = new() {NewLimit = int.MaxValue, EnterLimit = int.MaxValue};

        var mask = session.VisMask;
        var fromTick = session.FromTick;
        foreach (ref var ent in CollectionsMarshal.AsSpan(_cachedForceOverride))
        {
            ref var meta = ref _metadataMemory.GetRef(ent.Ptr.Index);
            if ((mask & meta.VisMask) == meta.VisMask)
                AddEntity(session, ref ent, ref meta, fromTick);
        }

        foreach (var uid in session.Viewers)
        {
            RecursivelyAddOverride(session, uid, fromTick, addChildren: false);
        }

        if (!_pvsOverride.SessionForceSend.TryGetValue(session.Session, out var sessionForce))
            return;

        foreach (var uid in sessionForce)
        {
            RecursivelyAddOverride(session, uid, fromTick, addChildren: false);
        }
    }

    private void RaiseExpandEvent(PvsSession session, GameTick fromTick)
    {
        var expandEvent = new ExpandPvsEvent(session.Session);

        if (session.Session.AttachedEntity != null)
            RaiseLocalEvent(session.Session.AttachedEntity.Value, ref expandEvent, true);
        else
            RaiseLocalEvent(ref expandEvent);

        if (expandEvent.Entities != null)
        {
            foreach (var uid in expandEvent.Entities)
            {
                RecursivelyAddOverride(session, uid, fromTick, addChildren: false);
            }
        }

        if (expandEvent.RecursiveEntities == null)
            return;

        foreach (var uid in expandEvent.RecursiveEntities)
        {
            RecursivelyAddOverride(session, uid, fromTick, addChildren: true);
        }
    }

    /// <summary>
    /// Recursively add an entity and all of its parents to the to-send set. This optionally also adds all children.
    /// </summary>
    private bool RecursivelyAddOverride(PvsSession session, EntityUid uid, GameTick fromTick, bool addChildren)
    {
        var xform = _xformQuery.GetComponent(uid);
        var parent = xform.ParentUid;

        // First we process all parents. This is because while this entity may already have been added
        // to the toSend set, it doesn't guarantee that its parents have been. E.g., if a player ghost just teleported
        // to follow a far away entity, the player's own entity is still being sent, but we need to ensure that we also
        // send the new parents, which may otherwise be delayed because of the PVS budget.
        if (parent.IsValid() && !RecursivelyAddOverride(session, parent, fromTick, false))
            return false;

        if (!_metaQuery.TryGetComponent(uid, out var meta))
            return false;

        if (!AddEntity(session, (uid, meta), fromTick))
            return false;

        if (addChildren)
            RecursivelyAddChildren(session, xform, fromTick);

        return true;
    }

    /// <summary>
    /// Recursively add an entity and all of its children to the to-send set.
    /// </summary>
    private void RecursivelyAddChildren(PvsSession session, TransformComponent xform, GameTick fromTick)
    {
        foreach (var child in xform._children)
        {
            if (!_xformQuery.TryGetComponent(child, out var childXform))
            {
                Log.Error($"Encountered deleted child {child} while recursively adding children.");
                continue;
            }

            var metadata = _metaQuery.GetComponent(child);
            if (!AddEntity(session, (child, metadata), fromTick))
                return;

            RecursivelyAddChildren(session, childXform, fromTick);
        }
    }

    private void CacheGlobalOverrides()
    {
        _cachedForceOverride.Clear();
        _forceOverrideSet.Clear();
        foreach (var uid in _pvsOverride.ForceSend)
        {
            CacheOverrideParents(uid, _cachedForceOverride, _forceOverrideSet, out _);
        }

        _cachedGlobalOverride.Clear();
        _globalOverrideSet.Clear();
        foreach (var uid in _pvsOverride.GlobalOverride)
        {
            CacheOverrideParents(uid, _cachedGlobalOverride, _globalOverrideSet, out var xform);
            CacheOverrideChildren(xform, _cachedGlobalOverride, _globalOverrideSet);
        }
    }

    private bool CacheOverrideParents(
        EntityUid uid,
        List<PvsChunk.ChunkEntity> list,
        HashSet<EntityUid> set,
        out TransformComponent xform)
    {
        xform = _xformQuery.GetComponent(uid);

        if (xform.ParentUid != EntityUid.Invalid && !CacheOverrideParents(xform.ParentUid, list, set, out _))
            return false;

        if (!set.Add(uid))
            return true;

        if (!_metaQuery.TryGetComponent(uid, out var meta))
        {
            Log.Error($"Encountered deleted entity in global overrides: {uid}");
            set.Remove(uid);
            return false;
        }

        list.Add(new(uid, meta));
        return true;
    }

    private void CacheOverrideChildren(TransformComponent xform, List<PvsChunk.ChunkEntity> list, HashSet<EntityUid> set)
    {
        foreach (var child in xform._children)
        {
            if (!_xformQuery.TryGetComponent(child, out var childXform))
            {
                Log.Error($"Encountered deleted child {child} while recursively adding children.");
                continue;
            }

            if (set.Add(child))
                list.Add(new(child, _metaQuery.GetComponent(child)));

            CacheOverrideChildren(childXform, list, set);
        }
    }
}
