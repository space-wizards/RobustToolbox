using System.Numerics;
using JetBrains.Annotations;
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
    [Pure]
    [PublicAPI]
    public Vector2 GetLinearVelocity(
        EntityUid uid,
        Vector2 point,
        PhysicsComponent? component = null,
        TransformComponent? xform = null)
    {
        if (!PhysicsQuery.Resolve(uid, ref component))
            return Vector2.Zero;

        if (!_xformQuery.Resolve(uid, ref xform))
            return Vector2.Zero;

        var velocity = component.LinearVelocity;
        var angVelocity = xform.LocalRotation.RotateVec(Vector2Helpers.Cross(component.AngularVelocity, point - component.LocalCenter));
        return velocity + angVelocity;
    }

    /// <summary>
    ///     This is the total rate of change of the coordinate's map position.
    /// </summary>
    [Pure]
    [PublicAPI]
    public Vector2 GetMapLinearVelocity(EntityCoordinates coordinates)
    {
        if (!coordinates.IsValid(EntityManager))
            return Vector2.Zero;

        var mapUid = _transform.GetMap(coordinates);
        var parent = coordinates.EntityId;
        var localPos = coordinates.Position;

        var velocity = Vector2.Zero;
        var angularComponent = Vector2.Zero;

        while (parent != mapUid && parent.IsValid())
        {
            // Could make this a method with the below one but ehh
            // then you get a method bigger than this block with a billion out args and who wants that.
            var xform = _xformQuery.GetComponent(parent);

            if (PhysicsQuery.TryGetComponent(parent, out var body))
            {
                velocity += body.LinearVelocity;
                angularComponent += Vector2Helpers.Cross(body.AngularVelocity, localPos - body.LocalCenter);
                angularComponent = xform.LocalRotation.RotateVec(angularComponent);
            }

            localPos = xform.LocalPosition + xform.LocalRotation.RotateVec(localPos);
            parent = xform.ParentUid;
        }

        return velocity;
    }

    /// <summary>
    ///     This is the total rate of change of the entity's map-position, resulting from the linear and angular
    ///     velocities of this entity and any parents.
    /// </summary>
    /// <remarks>
    ///     Use <see cref="GetMapVelocities"/> if you need linear and angular at the same time.
    /// </remarks>
    [Pure]
    [PublicAPI]
    public Vector2 GetMapLinearVelocity(
        EntityUid uid,
        PhysicsComponent? component = null,
        TransformComponent? xform = null)
    {
        if (!_xformQuery.Resolve(uid, ref xform))
            return Vector2.Zero;

        var parent = xform.ParentUid;
        var localPos = xform.LocalPosition;

        var velocity = component?.LinearVelocity ?? Vector2.Zero;
        Vector2 angularComponent = Vector2.Zero;

        while (parent != xform.MapUid && parent.IsValid())
        {
            xform = _xformQuery.GetComponent(parent);

            if (PhysicsQuery.TryGetComponent(parent, out var body))
            {
                // add linear velocity of parent relative to it's own parent (again, in map coordinates)
                velocity += body.LinearVelocity;

                // add angular velocity that results from the parent's rotation (NOT in map coordinates, but in the parentXform.Parent's frame)
                angularComponent += Vector2Helpers.Cross(body.AngularVelocity, localPos - body.LocalCenter);
                angularComponent = xform.LocalRotation.RotateVec(angularComponent);
            }

            localPos = xform.LocalPosition + xform.LocalRotation.RotateVec(localPos);
            parent = xform.ParentUid;
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
    [Pure]
    [PublicAPI]
    public float GetMapAngularVelocity(
        EntityUid uid,
        PhysicsComponent? component = null,
        TransformComponent? xform = null)
    {
        if (!PhysicsQuery.Resolve(uid, ref component))
            return 0;

        if (!_xformQuery.Resolve(uid, ref xform))
            return 0f;

        var angularVelocity = component.AngularVelocity;

        while (xform.ParentUid != xform.MapUid && xform.ParentUid.IsValid())
        {
            if (PhysicsQuery.TryGetComponent(xform.ParentUid, out var body))
                angularVelocity += body.AngularVelocity;

            xform = _xformQuery.GetComponent(xform.ParentUid);
        }

        return angularVelocity;
    }

    /// <summary>
    /// Gets the linear and angular velocity for this entity in map terms.
    /// </summary>
    [Pure]
    [PublicAPI]
    public (Vector2, float) GetMapVelocities(
        EntityUid uid,
        PhysicsComponent? component = null,
        TransformComponent? xform = null)
    {
        if (!PhysicsQuery.Resolve(uid, ref component))
            return (Vector2.Zero, 0);

        if (!_xformQuery.Resolve(uid, ref xform))
            return (Vector2.Zero, 0);

        var parent = xform.ParentUid;

        var localPos = xform.LocalPosition;

        var linearVelocity = component.LinearVelocity;
        var angularVelocity = component.AngularVelocity;
        Vector2 linearVelocityAngularContribution = Vector2.Zero;

        while (parent != xform.MapUid && parent.IsValid())
        {
            xform = _xformQuery.GetComponent(parent);

            if (PhysicsQuery.TryGetComponent(parent, out var body))
            {
                angularVelocity += body.AngularVelocity;

                // add linear velocity of parent relative to it's own parent (again, in map coordinates)
                linearVelocity += body.LinearVelocity;

                // add the component of the linear velocity that results from the parent's rotation. This is NOT in map
                // coordinates, this is the velocity in the parentXform.Parent's frame.
                linearVelocityAngularContribution += Vector2Helpers.Cross(body.AngularVelocity, localPos - body.LocalCenter);
                linearVelocityAngularContribution = xform.LocalRotation.RotateVec(linearVelocityAngularContribution);
            }

            localPos = xform.LocalPosition + xform.LocalRotation.RotateVec(localPos);
            parent = xform.ParentUid;
        }

        return (linearVelocity + linearVelocityAngularContribution, angularVelocity);
    }

    private void HandleParentChangeVelocity(EntityUid uid, PhysicsComponent physics, EntityUid oldParent, TransformComponent xform)
    {
        // If parent changed due to state handling, don't modify velocities. The physics comp state will take care of itself..
        if (_gameTiming.ApplyingState)
            return;

        if (physics.LifeStage != ComponentLifeStage.Running)
            return;

        if (physics.BodyType == BodyType.Static || xform.MapID == MapId.Nullspace || !physics.CanCollide)
            return;

        // When transferring bodies, we will preserve map angular and linear velocities. For this purpose, we simply
        // modify the velocities so that the map value remains unchanged.

        // TODO currently this causes issues when the parent changes due to teleportation or something like that. Though
        // I guess the question becomes, what do you do with conservation of momentum in that case. I guess its the job
        // of the teleporter to select a velocity at the after the parent has changed.

        FixturesComponent? manager = null;

        // for the new velocities (that need to be updated), we can just use the existing function:
        var (newLinear, newAngular) = GetMapVelocities(uid, physics, xform);

        // for the old velocities, we need to re-implement this function while using the old parent and old local position:
        if (oldParent == EntityUid.Invalid)
        {
            // no previous parent --> simple
            // Old velocity + (old velocity - new velocity)
            SetLinearVelocity(uid, physics.LinearVelocity * 2 - newLinear, manager: manager, body: physics);
            SetAngularVelocity(uid, physics.AngularVelocity * 2 - newAngular, manager: manager, body: physics);
            return;
        }

        var parent = oldParent;
        TransformComponent? parentXform = _xformQuery.GetComponent(parent);
        var localPos = Vector2.Transform(_transform.GetWorldPosition(xform), _transform.GetInvWorldMatrix(parentXform));

        var oldLinear = physics.LinearVelocity;
        var oldAngular = physics.AngularVelocity;
        Vector2 linearAngularContribution = Vector2.Zero;

        do
        {
            if (PhysicsQuery.TryGetComponent(parent, out var body))
            {
                oldAngular += body.AngularVelocity;

                // add linear velocity of parent relative to it's own parent (again, in map coordinates)
                oldLinear += body.LinearVelocity;

                // add the component of the linear velocity that results from the parent's rotation. This is NOT in map
                // coordinates, this is the velocity in the parentXform.Parent's frame.
                linearAngularContribution += Vector2Helpers.Cross(body.AngularVelocity, localPos - body.LocalCenter);
                linearAngularContribution = parentXform.LocalRotation.RotateVec(linearAngularContribution);
            }

            localPos = parentXform.LocalPosition + parentXform.LocalRotation.RotateVec(localPos);
            parent = parentXform.ParentUid;

        } while (parent.IsValid() && _xformQuery.TryGetComponent(parent, out parentXform));

        oldLinear += linearAngularContribution;

        // Finally we can update the Velocities. linear velocity is already in terms of map-coordinates, so no
        // world-rotation is required
        SetLinearVelocity(uid, physics.LinearVelocity + oldLinear - newLinear, manager: manager, body: physics);
        SetAngularVelocity(uid, physics.AngularVelocity + oldAngular - newAngular, manager: manager, body: physics);
    }
}
