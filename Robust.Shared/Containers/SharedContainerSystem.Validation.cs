using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Robust.Shared.Containers;


// This partial class just exists for debug asserts and bug fixing
public abstract partial class SharedContainerSystem : EntitySystem
{
    private void OnStartupValidation(EntityUid uid, ContainerManagerComponent component, ComponentStartup args)
    {
        foreach (var cont in component.Containers.Values)
        {
            foreach (var ent in cont.ContainedEntities)
            {
                if (!MetaQuery.TryGetComponent(ent, out var meta))
                {
                    ValidateMissingEntity(uid, cont, ent);
                    continue;
                }

                var xform = TransformQuery.GetComponent(ent);
                PhysicsQuery.TryGetComponent(ent, out var physics);

                DebugTools.Assert(xform.ParentUid == uid,
                    $"Entity not parented to its container. Entity: {ToPrettyString(ent)}, parent: {ToPrettyString(uid)}");
                DebugTools.Assert(!xform.Anchored,
                    $"Contained entity is anchored, Entity: {ToPrettyString(ent)}, parent: {ToPrettyString(uid)}");
                //DebugTools.Assert(physics == null || (!physics.Awake && !physics.CanCollide),
                //    $"Contained entity is can collide, Entity: {ToPrettyString(ent)}, parent: {ToPrettyString(uid)}");
                //DebugTools.Assert(xform.Broadphase == null,
                //    $"Contained entity is has non-null broadphase, Entity: {ToPrettyString(ent)}, parent: {ToPrettyString(uid)}");
                //DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) != 0,
                //    $"Contained entity is is missing container flag? Entity: {ToPrettyString(ent)}, parent: {ToPrettyString(uid)}");

                // TODO remove all the following and just have the above assert all wrapped in an #if DEBUG this is just here cause I
                // CBF updating all maps. meta-data flags now get saved, eventually we should be able to just initialize
                // entities in containers without having to "re-insert" them.
                meta.Flags |= MetaDataFlags.InContainer;
                _lookup.RemoveFromEntityTree(ent, xform);
                RecursivelyUpdatePhysics((ent, xform, physics));

                // assert children have correct properties
                ValidateChildren(xform, TransformQuery, PhysicsQuery);
            }
        }
    }

    protected abstract void ValidateMissingEntity(EntityUid uid, BaseContainer cont, EntityUid missing);

    private void ValidateChildren(TransformComponent xform, EntityQuery<TransformComponent> xformQuery, EntityQuery<PhysicsComponent> physicsQuery)
    {
        foreach (var child in xform._children)
        {
            if (!xformQuery.TryGetComponent(child, out var childXform))
                continue;

            DebugTools.Assert(!xform.Anchored,
                $"Child of contained entity is anchored, Entity: {ToPrettyString(child)}");
            DebugTools.Assert(!physicsQuery.TryGetComponent(child, out var physics) || (!physics.Awake && !physics.CanCollide),
                $"Child of contained entity is can collide, Entity: {ToPrettyString(child)}");
            DebugTools.Assert(xform.Broadphase == null,
                $"Child of contained entity is has non-null broadphase, Entity: {ToPrettyString(child)}");
            ValidateChildren(childXform, xformQuery, physicsQuery);
        }
    }

}
