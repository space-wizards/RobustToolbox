using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.GameObjects;

public sealed partial class EntityLookupSystem : EntitySystem
{
    // This logic could just be client-side but I CBF creating client/sever variants. The server should never run this code anyways.

    private readonly HashSet<EntityUid> _deferredUpdates = new();
    private readonly HashSet<EntityUid> _deferredTreeChanges = new();
    private readonly HashSet<EntityUid> _deferredAdditions = new();
    private readonly HashSet<EntityUid> _deferredRemoval = new();

    public (int Updates, int Changes, int Insertions, int Removal) ProcessDeferredUpdates()
    {
        _deferredUpdates.ExceptWith(_deferredTreeChanges);
        _deferredUpdates.ExceptWith(_deferredAdditions);
        _deferredUpdates.ExceptWith(_deferredRemoval);

        _deferredTreeChanges.UnionWith(_deferredAdditions.Intersect(_deferredRemoval));
        _deferredAdditions.ExceptWith(_deferredTreeChanges);
        _deferredRemoval.ExceptWith(_deferredTreeChanges);

        DebugTools.Assert(_deferredUpdates.Intersect(_deferredAdditions).Count() == 0);
        DebugTools.Assert(_deferredTreeChanges.Intersect(_deferredAdditions).Count() == 0);

        var xformQuery = GetEntityQuery<TransformComponent>();
        var metaQuery = GetEntityQuery<MetaDataComponent>();
        var contQuery = GetEntityQuery<ContainerManagerComponent>();
        var physicsQuery = GetEntityQuery<PhysicsComponent>();
        var fixturesQuery = GetEntityQuery<FixturesComponent>();
        var broadQuery = GetEntityQuery<BroadphaseComponent>();

        var updates = _deferredUpdates.Count;
        var changes = _deferredTreeChanges.Count;
        var insertions = _deferredAdditions.Count;
        var removal = _deferredRemoval.Count;
        try
        {
            foreach (var uid in _deferredUpdates)
            {
                var xform = xformQuery.GetComponent(uid);
                if (!TryGetCurrentBroadphase(xform, out var broadphase))
                    continue;

                var broadphaseXform = xformQuery.GetComponent(broadphase.Owner);
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

            foreach (var uid in _deferredTreeChanges)
            {
                var xform = xformQuery.GetComponent(uid);
                if (!TryGetCurrentBroadphase(xform, out var oldBroadphase))
                    continue;

                TryFindBroadphase(xform, broadQuery, xformQuery, out var newBroadphase);

                if (oldBroadphase != null && oldBroadphase != newBroadphase)
                {
                    var oldBroadphaseXform = xformQuery.GetComponent(oldBroadphase.Owner);
                    if (!TryComp(oldBroadphaseXform.MapUid, out SharedPhysicsMapComponent? oldPhysMap))
                    {
                        throw new InvalidOperationException(
                            $"Oldd broadphase's map is missing a physics map comp. Broadphase: {ToPrettyString(oldBroadphase.Owner)}");
                    }

                    RemoveFromEntityTree(oldBroadphase.Owner, oldBroadphase, oldBroadphaseXform, oldPhysMap, uid, xform, xformQuery, physicsQuery, fixturesQuery);
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

            foreach (var uid in _deferredAdditions)
            {
                var xform = xformQuery.GetComponent(uid);
                if (!TryFindBroadphase(xform, broadQuery, xformQuery, out var broadphase))
                    continue;

                var broadphaseXform = xformQuery.GetComponent(broadphase.Owner);
                if (!TryComp(broadphaseXform.MapUid, out SharedPhysicsMapComponent? physMap))
                {
                    throw new InvalidOperationException(
                        $"Broadphase's map is missing a physics map comp. Broadphase: {ToPrettyString(broadphase.Owner)}");
                }

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

            foreach (var uid in _deferredRemoval)
            {
                var xform = xformQuery.GetComponent(uid);
                if (!TryGetCurrentBroadphase(xform, out var broadphase))
                    continue;

                var broadphaseXform = xformQuery.GetComponent(broadphase.Owner);
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
            _deferredTreeChanges.Clear();
            _deferredAdditions.Clear();
            _deferredUpdates.Clear();
            _deferredRemoval.Clear();
            throw;
        }

        _deferredTreeChanges.Clear();
        _deferredAdditions.Clear();
        _deferredUpdates.Clear();
        _deferredRemoval.Clear();
        return (updates, changes, insertions, removal);
    }
}
