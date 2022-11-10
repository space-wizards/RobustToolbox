using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    /// <summary>
    /// Gets the linear velocity of a particular body at the specified point.
    /// </summary>
    public Vector2 GetLinearVelocity(
        EntityUid uid,
        Vector2 point,
        PhysicsComponent? component = null,
        TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref component, ref xform))
            return Vector2.Zero;

        var velocity = component.LinearVelocity;
        var angVelocity = xform.LocalRotation.RotateVec(Vector2.Cross(component.AngularVelocity, point - component.LocalCenter));
        return velocity + angVelocity;
    }

    /// <summary>
    ///     This is the total rate of change of the entity's map-position, resulting from the linear and angular
    ///     velocities of this entity and any parents.
    /// </summary>
    /// <remarks>
    ///     Use <see cref="GetMapVelocities"/> if you need linear and angular at the same time.
    /// </remarks>
    public Vector2 GetMapLinearVelocity(
        EntityUid uid,
        PhysicsComponent? component = null,
        TransformComponent? xform = null,
        EntityQuery<TransformComponent>? xformQuery = null,
        EntityQuery<PhysicsComponent>? physicsQuery = null)
    {
        if (!Resolve(uid, ref component))
            return Vector2.Zero;

        xformQuery ??= EntityManager.GetEntityQuery<TransformComponent>();
        physicsQuery ??= EntityManager.GetEntityQuery<PhysicsComponent>();

        xform ??= xformQuery.Value.GetComponent(uid);
        var parent = xform.ParentUid;
        var localPos = xform.LocalPosition;

        var velocity = component.LinearVelocity;
        Vector2 angularComponent = Vector2.Zero;

        while (parent.IsValid())
        {
            var parentXform = xformQuery.Value.GetComponent(parent);

            if (physicsQuery.Value.TryGetComponent(parent, out var body))
            {
                // add linear velocity of parent relative to it's own parent (again, in map coordinates)
                velocity += body.LinearVelocity;

                // add angular velocity that results from the parent's rotation (NOT in map coordinates, but in the parentXform.Parent's frame)
                angularComponent += Vector2.Cross(body.AngularVelocity, localPos - body.LocalCenter);
                angularComponent = parentXform.LocalRotation.RotateVec(angularComponent);
            }

            localPos = parentXform.LocalPosition + parentXform.LocalRotation.RotateVec(localPos);
            parent = parentXform.ParentUid;
        }

        // angular component of the velocity should now be in terms of map coordinates and can be added onto the sum of
        // linear velocities.
        return velocity + angularComponent;
    }

    /// <summary>
    /// Get the body's total angular velocity. This is the rate of change of the entity's world rotation.
    /// </summary>
    /// <remarks>
    /// Consider using <see cref="GetMapVelocities"/> if you need linear and angular at the same time.
    /// </remarks>
    public float GetMapAngularVelocity(
        EntityUid uid,
        PhysicsComponent? component = null,
        TransformComponent? xform = null,
        EntityQuery<TransformComponent>? xformQuery = null,
        EntityQuery<PhysicsComponent>? physicsQuery = null)
    {
        if (!Resolve(uid, ref component))
            return 0;

        xformQuery ??= EntityManager.GetEntityQuery<TransformComponent>();
        physicsQuery ??= EntityManager.GetEntityQuery<PhysicsComponent>();

        xform ??= xformQuery.Value.GetComponent(uid);
        var parent = xform.ParentUid;

        var angularVelocity = component.AngularVelocity;

        while (parent.IsValid())
        {
            var parentXform = xformQuery.Value.GetComponent(parent);

            if (physicsQuery.Value.TryGetComponent(parent, out var body))
                angularVelocity += body.AngularVelocity;
            parent = parentXform.ParentUid;
        }

        return angularVelocity;
    }

    public (Vector2, float) GetMapVelocities(
        EntityUid uid,
        PhysicsComponent? component = null,
        TransformComponent? xform = null,
        EntityQuery<TransformComponent>? xformQuery = null,
        EntityQuery<PhysicsComponent>? physicsQuery = null)
    {
        if (!Resolve(uid, ref component))
            return (Vector2.Zero, 0);

        xformQuery ??= EntityManager.GetEntityQuery<TransformComponent>();
        physicsQuery ??= EntityManager.GetEntityQuery<PhysicsComponent>();

        xform ??= xformQuery.Value.GetComponent(uid);
        var parent = xform.ParentUid;

        var localPos = xform.LocalPosition;

        var linearVelocity = component.LinearVelocity;
        var angularVelocity = component.AngularVelocity;
        Vector2 linearVelocityAngularContribution = Vector2.Zero;

        while (parent.IsValid())
        {
            var parentXform = xformQuery.Value.GetComponent(parent);

            if (physicsQuery.Value.TryGetComponent(parent, out var body))
            {
                angularVelocity += body.AngularVelocity;

                // add linear velocity of parent relative to it's own parent (again, in map coordinates)
                linearVelocity += body.LinearVelocity;

                // add the component of the linear velocity that results from the parent's rotation. This is NOT in map
                // coordinates, this is the velocity in the parentXform.Parent's frame.
                linearVelocityAngularContribution += Vector2.Cross(body.AngularVelocity, localPos - body.LocalCenter);
                linearVelocityAngularContribution = parentXform.LocalRotation.RotateVec(linearVelocityAngularContribution);
            }

            localPos = parentXform.LocalPosition + parentXform.LocalRotation.RotateVec(localPos);
            parent = parentXform.ParentUid;
        }

        return (linearVelocity + linearVelocityAngularContribution, angularVelocity);
    }

    private void HandleParentChangeVelocity(EntityUid uid, PhysicsComponent physics, ref EntParentChangedMessage args, TransformComponent xform)
    {
        // If parent changed due to state handling, don't modify velocities. The physics comp state will take care of itself..
        if (_gameTiming.ApplyingState)
            return;

        if (physics.LifeStage != ComponentLifeStage.Running)
            return;

        if (physics.BodyType == BodyType.Static || xform.MapID == MapId.Nullspace || !physics._canCollide)
            return;

        // When transferring bodies, we will preserve map angular and linear velocities. For this purpose, we simply
        // modify the velocities so that the map value remains unchanged.

        // TODO currently this causes issues when the parent changes due to teleportation or something like that. Though
        // I guess the question becomes, what do you do with conservation of momentum in that case. I guess its the job
        // of the teleporter to select a velocity at the after the parent has changed.

        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        var physicsQuery = EntityManager.GetEntityQuery<PhysicsComponent>();

        // for the new velocities (that need to be updated), we can just use the existing function:
        var (newLinear, newAngular) = GetMapVelocities(uid, physics, xform, xformQuery, physicsQuery);

        // for the old velocities, we need to re-implement this function while using the old parent and old local position:
        if (args.OldParent is not { Valid: true } parent)
        {
            // no previous parent --> simple
            // Old velocity + (old velocity - new velocity)
            SetLinearVelocity(physics, physics._linearVelocity * 2 - newLinear);
            SetAngularVelocity(physics, physics._angularVelocity * 2 - newAngular);
            return;
        }

        TransformComponent? parentXform = xformQuery.GetComponent(parent);
        var localPos = parentXform.InvWorldMatrix.Transform(xform.WorldPosition);

        var oldLinear = physics.LinearVelocity;
        var oldAngular = physics.AngularVelocity;
        Vector2 linearAngularContribution = Vector2.Zero;

        do
        {
            if (physicsQuery.TryGetComponent(parent, out var body))
            {
                oldAngular += body.AngularVelocity;

                // add linear velocity of parent relative to it's own parent (again, in map coordinates)
                oldLinear += body.LinearVelocity;

                // add the component of the linear velocity that results from the parent's rotation. This is NOT in map
                // coordinates, this is the velocity in the parentXform.Parent's frame.
                linearAngularContribution += Vector2.Cross(body.AngularVelocity, localPos - body.LocalCenter);
                linearAngularContribution = parentXform.LocalRotation.RotateVec(linearAngularContribution);
            }

            localPos = parentXform.LocalPosition + parentXform.LocalRotation.RotateVec(localPos);
            parent = parentXform.ParentUid;

        } while (parent.IsValid() && xformQuery.TryGetComponent(parent, out parentXform));

        oldLinear += linearAngularContribution;

        // Finally we can update the Velocities. linear velocity is already in terms of map-coordinates, so no
        // world-rotation is required
        SetLinearVelocity(physics, physics._linearVelocity + oldLinear - newLinear);
        SetAngularVelocity(physics, physics._angularVelocity + oldAngular - newAngular);
    }
}
