using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using System;
using System.Collections.Generic;

namespace Robust.Shared.GameObjects;

public sealed partial class EntityLookupSystem : EntitySystem
{
    // This logic could just be client-side but I CBF creating client/sever variants. The server should never run this code anyways.

    private readonly HashSet<EntityUid> _deferredBroadChanges = new(); // update broadphase (or just tree)
    private readonly HashSet<EntityUid> _deferredRemoval = new(); // remove from a broadphase
    private readonly HashSet<EntityUid> _deferredUpdates = new(); // Update position

    public (int Updates, int Changes, int Removals) ProcessDeferredUpdates()
    {
        _deferredRemoval.ExceptWith(_deferredBroadChanges);
        _deferredUpdates.ExceptWith(_deferredBroadChanges);
        _deferredUpdates.ExceptWith(_deferredRemoval);

        var xformQuery = GetEntityQuery<TransformComponent>();
        var metaQuery = GetEntityQuery<MetaDataComponent>();
        var contQuery = GetEntityQuery<ContainerManagerComponent>();
        var physicsQuery = GetEntityQuery<PhysicsComponent>();
        var fixturesQuery = GetEntityQuery<FixturesComponent>();
        var broadQuery = GetEntityQuery<BroadphaseComponent>();

        var updates = _deferredUpdates.Count;
        var changes = _deferredBroadChanges.Count;
        var removals = _deferredRemoval.Count;
        try
        {
            foreach (var uid in _deferredUpdates)
            {
                if (!xformQuery.TryGetComponent(uid, out var xform))
                    continue;

                if (!TryGetCurrentBroadphase(xform, out var broadphase))
                    continue;

                var broadphaseXform = xformQuery.GetComponent(broadphase.Owner);

                if (broadphaseXform.MapID != MapId.Nullspace)
                    continue;

                if (!TryComp(broadphaseXform.MapUid, out SharedPhysicsMapComponent? physMap))
                {
                    throw new InvalidOperationException(
                        $"Broadphase's map is missing a physics map comp. Broadphase: {ToPrettyString(broadphase.Owner)}");
                }

                // TODO ensure children are removed from _deferredUpdates?
                AddToEntityTree(
                    broadphase.Owner,
                    broadphase,
                    broadphaseXform,
                    physMap,
                    uid,
                    xform,
                    xformQuery,
                    metaQuery,
                    contQuery,
                    physicsQuery,
                    fixturesQuery);
            }

            foreach (var uid in _deferredBroadChanges)
            {
                if (!xformQuery.TryGetComponent(uid, out var xform))
                    continue;

                TryGetCurrentBroadphase(xform, out var oldBroadphase);

                // TODO separate change-tree from change-broadphase
                // i.e., if just changing can-collide, we can avoid this call altogether.
                TryFindBroadphase(xform, broadQuery, xformQuery, out var newBroadphase);

                if (oldBroadphase != null && oldBroadphase != newBroadphase)
                {
                    var oldBroadphaseXform = xformQuery.GetComponent(oldBroadphase.Owner);

                    if (oldBroadphaseXform.MapID != MapId.Nullspace)
                    {
                        if (!TryComp(oldBroadphaseXform.MapUid, out SharedPhysicsMapComponent? oldPhysMap))
                        {
                            throw new InvalidOperationException(
                                $"Oldd broadphase's map is missing a physics map comp. Broadphase: {ToPrettyString(oldBroadphase.Owner)}");
                        }

                        RemoveFromEntityTree(oldBroadphase.Owner, oldBroadphase, oldBroadphaseXform, oldPhysMap, uid, xform, xformQuery, physicsQuery, fixturesQuery);
                    }
                }

                if (newBroadphase == null)
                    continue;

                var newBroadphaseXform = xformQuery.GetComponent(newBroadphase.Owner);
                if (!TryComp(newBroadphaseXform.MapUid, out SharedPhysicsMapComponent? physMap))
                {
                    throw new InvalidOperationException(
                        $"Broadphase's map is missing a physics map comp. Broadphase: {ToPrettyString(newBroadphase.Owner)}");
                }

                AddToEntityTree(
                    newBroadphase.Owner,
                    newBroadphase,
                    newBroadphaseXform,
                    physMap,
                    uid,
                    xform,
                    xformQuery,
                    metaQuery,
                    contQuery,
                    physicsQuery,
                    fixturesQuery);
            }

            foreach (var uid in _deferredRemoval)
            {
                if (!xformQuery.TryGetComponent(uid, out var xform))
                    continue;

                if (!TryGetCurrentBroadphase(xform, out var broadphase))
                    continue;

                var broadphaseXform = xformQuery.GetComponent(broadphase.Owner);
                if (broadphaseXform.MapID != MapId.Nullspace)
                    continue;

                if (!TryComp(broadphaseXform.MapUid, out SharedPhysicsMapComponent? physMap))
                {
                    throw new InvalidOperationException(
                        $"Broadphase's map is missing a physics map comp. Broadphase: {ToPrettyString(broadphase.Owner)}");
                }

                RemoveFromEntityTree(broadphase.Owner, broadphase, broadphaseXform, physMap, uid, xform, xformQuery, physicsQuery, fixturesQuery);
            }
        }
        catch
        {
            _deferredBroadChanges.Clear();
            _deferredUpdates.Clear();
            _deferredRemoval.Clear();
            throw;
        }

        _deferredBroadChanges.Clear();
        _deferredUpdates.Clear();
        _deferredRemoval.Clear();
        return (updates, changes, removals);
    }
}
